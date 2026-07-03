using IdealWeightNutrition.Application.Abstractions;
using IdealWeightNutrition.Contracts.Reviews;
using IdealWeightNutrition.Domain.Catalogue;
using IdealWeightNutrition.Domain.Constants;
using IdealWeightNutrition.Domain.Engagement;
using IdealWeightNutrition.Infrastructure.Options;
using IdealWeightNutrition.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace IdealWeightNutrition.Infrastructure.Services;

internal sealed class ReviewService : IReviewService
{
    private readonly AppDbContext _db;
    private readonly IDateTimeProvider _clock;
    private readonly SiteSettingsOptions _siteSettings;

    public ReviewService(AppDbContext db, IDateTimeProvider clock, IOptions<SiteSettingsOptions> siteSettings)
    {
        _db = db;
        _clock = clock;
        _siteSettings = siteSettings.Value;
    }

    public async Task<IReadOnlyList<ProductReviewDto>> ListApprovedProductReviewsAsync(
        int productId,
        CancellationToken cancellationToken = default)
    {
        var reviews = await _db.Reviews
            .AsNoTracking()
            .Where(r => r.ProductId == productId && r.IsApproved)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync(cancellationToken);

        return await MapProductReviewsAsync(reviews, cancellationToken);
    }

    public async Task<ProductReviewSummaryDto> GetProductReviewSummaryAsync(
        int productId,
        CancellationToken cancellationToken = default)
    {
        var reviews = await _db.Reviews
            .AsNoTracking()
            .Where(r => r.ProductId == productId && r.IsApproved)
            .Select(r => r.Rating)
            .ToListAsync(cancellationToken);

        return new ProductReviewSummaryDto
        {
            AverageRating = reviews.Count == 0 ? 0 : Math.Round(reviews.Average(), 1),
            ReviewCount = reviews.Count
        };
    }

    public async Task<ProductReviewDto> SubmitProductReviewAsync(
        int productId,
        string userId,
        SubmitProductReviewRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.Rating is < 1 or > 5)
            throw new InvalidOperationException("Rating must be between 1 and 5.");
        if (string.IsNullOrWhiteSpace(request.Comment) || request.Comment.Trim().Length < 10)
            throw new InvalidOperationException("Review must be at least 10 characters.");

        var productExists = await _db.Products.AnyAsync(p => p.Id == productId && !p.IsDeleted, cancellationToken);
        if (!productExists)
            throw new InvalidOperationException("Product not found.");

        var alreadyReviewed = await _db.Reviews.AnyAsync(
            r => r.ProductId == productId && r.UserId == userId,
            cancellationToken);
        if (alreadyReviewed)
            throw new InvalidOperationException("You have already reviewed this product.");

        var hasPurchased = _siteSettings.EnableReviewWithoutOrder
            || await _db.OrderDetails.AnyAsync(
                d => d.ProductId == productId
                     && _db.OrderHeaders.Any(o =>
                         o.Id == d.OrderHeaderId
                         && o.ApplicationUserId == userId
                         && o.OrderStatus == OrderStatuses.Delivered),
                cancellationToken);

        var review = new Review
        {
            ProductId = productId,
            UserId = userId,
            Rating = request.Rating,
            Comment = request.Comment.Trim(),
            CreatedAt = _clock.Now,
            IsApproved = false,
            IsVerifiedPurchase = hasPurchased,
            HelpfulCount = 0
        };

        _db.Reviews.Add(review);
        await _db.SaveChangesAsync(cancellationToken);

        var mapped = await MapProductReviewsAsync([review], cancellationToken);
        return mapped[0];
    }

    public async Task<IReadOnlyList<ProductReviewDto>> ListApprovedServiceReviewsAsync(
        int serviceId,
        CancellationToken cancellationToken = default)
    {
        var reviews = await _db.Reviews
            .AsNoTracking()
            .Where(r => r.ServiceSubscriptionId == serviceId && r.IsApproved)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync(cancellationToken);

        return await MapProductReviewsAsync(reviews, cancellationToken);
    }

    public async Task<ProductReviewSummaryDto> GetServiceReviewSummaryAsync(
        int serviceId,
        CancellationToken cancellationToken = default)
    {
        var reviews = await _db.Reviews
            .AsNoTracking()
            .Where(r => r.ServiceSubscriptionId == serviceId && r.IsApproved)
            .Select(r => r.Rating)
            .ToListAsync(cancellationToken);

        return new ProductReviewSummaryDto
        {
            AverageRating = reviews.Count == 0 ? 0 : Math.Round(reviews.Average(), 1),
            ReviewCount = reviews.Count
        };
    }

    public async Task<ProductReviewDto> SubmitServiceReviewAsync(
        int serviceId,
        string userId,
        SubmitProductReviewRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.Rating is < 1 or > 5)
            throw new InvalidOperationException("Rating must be between 1 and 5.");
        if (string.IsNullOrWhiteSpace(request.Comment) || request.Comment.Trim().Length < 10)
            throw new InvalidOperationException("Review must be at least 10 characters.");

        var serviceExists = await _db.ServiceSubscriptions.AnyAsync(
            s => s.Id == serviceId && s.IsActive,
            cancellationToken);
        if (!serviceExists)
            throw new InvalidOperationException("Service not found.");

        var alreadyReviewed = await _db.Reviews.AnyAsync(
            r => r.ServiceSubscriptionId == serviceId && r.UserId == userId,
            cancellationToken);
        if (alreadyReviewed)
            throw new InvalidOperationException("You have already reviewed this service.");

        var hasPurchased = _siteSettings.EnableReviewWithoutOrder
            || await _db.ServicePurchases.AnyAsync(
                sp => sp.ServiceSubscriptionId == serviceId
                      && sp.ApplicationUserId == userId
                      && (sp.PaymentStatus == PaymentStatuses.Paid
                          || sp.PaymentStatus == PaymentStatuses.DelayedPayment
                          || sp.PaymentStatus == "Approved"),
                cancellationToken);

        var review = new Review
        {
            ServiceSubscriptionId = serviceId,
            UserId = userId,
            Rating = request.Rating,
            Comment = request.Comment.Trim(),
            CreatedAt = _clock.Now,
            IsApproved = false,
            IsVerifiedPurchase = hasPurchased,
            HelpfulCount = 0
        };

        _db.Reviews.Add(review);
        await _db.SaveChangesAsync(cancellationToken);

        var mapped = await MapProductReviewsAsync([review], cancellationToken);
        return mapped[0];
    }

    public async Task<IReadOnlyList<FeaturedReviewDto>> ListFeaturedReviewsAsync(
        int count = 6,
        CancellationToken cancellationToken = default)
    {
        var take = Math.Clamp(count, 1, 12);
        var reviews = await _db.Reviews
            .AsNoTracking()
            .Where(r => r.IsApproved && r.Comment.Length >= 10)
            .OrderByDescending(r => r.IsVerifiedPurchase)
            .ThenByDescending(r => r.Rating)
            .ThenByDescending(r => r.CreatedAt)
            .Take(take)
            .ToListAsync(cancellationToken);

        if (reviews.Count == 0)
            return [];

        var userIds = reviews.Select(r => r.UserId).Distinct().ToList();
        var users = await _db.Users.AsNoTracking()
            .Where(u => userIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, cancellationToken);

        return reviews.Select(r =>
        {
            users.TryGetValue(r.UserId, out var user);
            return new FeaturedReviewDto
            {
                Id = r.Id,
                UserName = user?.Name ?? user?.UserName ?? "Customer",
                Location = FormatUserLocation(user?.City, user?.State),
                Rating = r.Rating,
                Comment = r.Comment,
                IsVerifiedPurchase = r.IsVerifiedPurchase
            };
        }).ToList();
    }

    public async Task<IReadOnlyList<AdminReviewListItemDto>> ListAdminReviewsAsync(
        string? status,
        CancellationToken cancellationToken = default)
    {
        var query = _db.Reviews.AsNoTracking();

        if (string.Equals(status, "pending", StringComparison.OrdinalIgnoreCase))
            query = query.Where(r => !r.IsApproved);
        else if (string.Equals(status, "approved", StringComparison.OrdinalIgnoreCase))
            query = query.Where(r => r.IsApproved);

        var reviews = await query.OrderByDescending(r => r.CreatedAt).Take(200).ToListAsync(cancellationToken);
        if (reviews.Count == 0)
            return [];

        var productIds = reviews.Where(r => r.ProductId.HasValue).Select(r => r.ProductId!.Value).Distinct().ToList();
        var products = await _db.Products.AsNoTracking()
            .Where(p => productIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, cancellationToken);

        var serviceIds = reviews.Where(r => r.ServiceSubscriptionId.HasValue).Select(r => r.ServiceSubscriptionId!.Value).Distinct().ToList();
        var services = await _db.ServiceSubscriptions.AsNoTracking()
            .Where(s => serviceIds.Contains(s.Id))
            .ToDictionaryAsync(s => s.Id, cancellationToken);

        var userIds = reviews.Select(r => r.UserId).Distinct().ToList();
        var users = await _db.Users.AsNoTracking()
            .Where(u => userIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, cancellationToken);

        return reviews.Select(r =>
        {
            products.TryGetValue(r.ProductId ?? 0, out var product);
            services.TryGetValue(r.ServiceSubscriptionId ?? 0, out var service);
            users.TryGetValue(r.UserId, out var user);
            return new AdminReviewListItemDto
            {
                Id = r.Id,
                ProductId = r.ProductId,
                ProductTitle = product?.Title,
                ServiceSubscriptionId = r.ServiceSubscriptionId,
                ServiceTitle = service?.Title,
                ReviewType = r.ServiceSubscriptionId.HasValue ? "Service" : "Product",
                UserName = user?.Name ?? user?.UserName ?? user?.Email ?? "Anonymous",
                Rating = r.Rating,
                Comment = r.Comment,
                CreatedAt = r.CreatedAt,
                IsApproved = r.IsApproved,
                IsVerifiedPurchase = r.IsVerifiedPurchase
            };
        }).ToList();
    }

    public async Task ToggleApprovalAsync(int reviewId, CancellationToken cancellationToken = default)
    {
        var review = await _db.Reviews.FirstOrDefaultAsync(r => r.Id == reviewId, cancellationToken)
            ?? throw new InvalidOperationException("Review not found.");

        review.IsApproved = !review.IsApproved;
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteReviewAsync(int reviewId, CancellationToken cancellationToken = default)
    {
        var review = await _db.Reviews.FirstOrDefaultAsync(r => r.Id == reviewId, cancellationToken)
            ?? throw new InvalidOperationException("Review not found.");

        _db.Reviews.Remove(review);
        await _db.SaveChangesAsync(cancellationToken);
    }

    private static string? FormatUserLocation(string? city, string? state)
    {
        if (string.IsNullOrWhiteSpace(city))
            return string.IsNullOrWhiteSpace(state) ? null : state.Trim();

        return string.IsNullOrWhiteSpace(state)
            ? city.Trim()
            : $"{city.Trim()}, {state.Trim()}";
    }

    private async Task<IReadOnlyList<ProductReviewDto>> MapProductReviewsAsync(
        IReadOnlyList<Review> reviews,
        CancellationToken cancellationToken)
    {
        if (reviews.Count == 0)
            return [];

        var userIds = reviews.Select(r => r.UserId).Distinct().ToList();
        var users = await _db.Users.AsNoTracking()
            .Where(u => userIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, cancellationToken);

        return reviews.Select(r =>
        {
            users.TryGetValue(r.UserId, out var user);
            return new ProductReviewDto
            {
                Id = r.Id,
                UserName = user?.Name ?? user?.UserName ?? "Anonymous",
                Rating = r.Rating,
                Comment = r.Comment,
                CreatedAt = r.CreatedAt,
                IsVerifiedPurchase = r.IsVerifiedPurchase,
                HelpfulCount = r.HelpfulCount
            };
        }).ToList();
    }
}
