using IdealWeightNutrition.Application.Abstractions;
using IdealWeightNutrition.Contracts.Admin;
using IdealWeightNutrition.Domain.Catalogue;
using IdealWeightNutrition.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace IdealWeightNutrition.Infrastructure.Services;

internal sealed class AdminStockNotificationService : IAdminStockNotificationService
{
    private readonly AppDbContext _db;
    private readonly IDateTimeProvider _clock;

    public AdminStockNotificationService(AppDbContext db, IDateTimeProvider clock)
    {
        _db = db;
        _clock = clock;
    }

    public async Task<AdminStockNotificationListResponse> ListAsync(
        AdminStockNotificationQuery query,
        CancellationToken cancellationToken = default)
    {
        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, 100);

        var filtered = BuildQuery(query);
        var total = await filtered.CountAsync(cancellationToken);

        var rows = await filtered
            .OrderByDescending(s => s.CreatedDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var productIds = rows.Select(r => r.ProductId).Distinct().ToList();
        var variantIds = rows
            .Where(r => r.ProductVariantId.HasValue)
            .Select(r => r.ProductVariantId!.Value)
            .Distinct()
            .ToList();

        var products = productIds.Count == 0
            ? new Dictionary<int, Product>()
            : await _db.Products.AsNoTracking()
                .Where(p => productIds.Contains(p.Id))
                .ToDictionaryAsync(p => p.Id, cancellationToken);

        var variants = variantIds.Count == 0
            ? new Dictionary<int, ProductVariant>()
            : await _db.Set<ProductVariant>().AsNoTracking()
                .Where(v => variantIds.Contains(v.Id))
                .ToDictionaryAsync(v => v.Id, cancellationToken);

        var items = rows.Select(row =>
        {
            products.TryGetValue(row.ProductId, out var product);
            string? sku = null;
            if (row.ProductVariantId is int variantId && variants.TryGetValue(variantId, out var variant))
                sku = variant.Sku;

            return new AdminStockNotificationListItemDto
            {
                Id = row.Id,
                ProductId = row.ProductId,
                ProductTitle = product?.Title ?? $"Product #{row.ProductId}",
                ProductVariantId = row.ProductVariantId,
                VariantSku = sku,
                Email = row.Email,
                PhoneNumber = row.PhoneNumber,
                IsActive = row.IsActive,
                IsNotified = row.IsNotified,
                NotifiedDate = row.NotifiedDate,
                CreatedDate = row.CreatedDate
            };
        }).ToList();

        return new AdminStockNotificationListResponse
        {
            Items = items,
            TotalCount = total,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<AdminStockNotificationActionResponse> DeactivateAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        var notification = await _db.StockNotifications
            .FirstOrDefaultAsync(s => s.Id == id && !s.IsDeleted, cancellationToken)
            ?? throw new InvalidOperationException("Stock notification not found.");

        if (!notification.IsActive)
            return new AdminStockNotificationActionResponse
            {
                Id = notification.Id,
                Message = "Subscription is already inactive."
            };

        notification.IsActive = false;
        notification.ModifiedDate = _clock.Now;
        await _db.SaveChangesAsync(cancellationToken);

        return new AdminStockNotificationActionResponse
        {
            Id = notification.Id,
            Message = "Subscription deactivated."
        };
    }

    private IQueryable<Domain.Engagement.StockNotification> BuildQuery(AdminStockNotificationQuery query)
    {
        var q = _db.StockNotifications.AsNoTracking().Where(s => !s.IsDeleted);

        if (query.ActiveOnly)
            q = q.Where(s => s.IsActive);

        if (query.PendingOnly)
            q = q.Where(s => !s.IsNotified);

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = query.Search.Trim().ToLowerInvariant();
            var matchingProductIds = _db.Products.AsNoTracking()
                .Where(p => p.Title.ToLower().Contains(term))
                .Select(p => p.Id);

            q = q.Where(s =>
                s.Id.ToString().Contains(term)
                || s.Email.ToLower().Contains(term)
                || (s.PhoneNumber != null && s.PhoneNumber.Contains(term))
                || matchingProductIds.Contains(s.ProductId));
        }

        return q;
    }
}
