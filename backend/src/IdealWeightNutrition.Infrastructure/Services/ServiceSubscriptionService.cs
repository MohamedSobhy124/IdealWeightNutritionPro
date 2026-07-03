using IdealWeightNutrition.Application.Abstractions;
using IdealWeightNutrition.Contracts.Services;
using IdealWeightNutrition.Domain.Services;
using IdealWeightNutrition.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace IdealWeightNutrition.Infrastructure.Services;

internal sealed class ServiceSubscriptionService : IServiceSubscriptionService
{
    private readonly AppDbContext _db;
    private readonly IDateTimeProvider _clock;

    public ServiceSubscriptionService(AppDbContext db, IDateTimeProvider clock)
    {
        _db = db;
        _clock = clock;
    }

    public async Task<IReadOnlyList<ServiceSubscriptionSummaryDto>> ListActiveAsync(
        CancellationToken cancellationToken = default)
    {
        var now = _clock.Now;
        var services = await _db.ServiceSubscriptions
            .AsNoTracking()
            .Include(s => s.Images)
            .Include(s => s.Offers)
            .Where(s => s.IsActive)
            .OrderBy(s => s.DisplayOrder)
            .ThenByDescending(s => s.CreatedDate)
            .ToListAsync(cancellationToken);

        return services.Select(s => MapSummary(s, now)).ToList();
    }

    public async Task<ServiceSubscriptionDetailDto?> GetActiveAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        var now = _clock.Now;
        var service = await _db.ServiceSubscriptions
            .AsNoTracking()
            .Include(s => s.Images)
            .Include(s => s.Offers)
            .FirstOrDefaultAsync(s => s.Id == id && s.IsActive, cancellationToken);

        if (service is null)
            return null;

        var summary = MapSummary(service, now);
        var imageUrls = BuildImageUrls(service);

        return new ServiceSubscriptionDetailDto
        {
            Id = summary.Id,
            Title = summary.Title,
            TitleAr = summary.TitleAr,
            Description = summary.Description,
            DescriptionAr = summary.DescriptionAr,
            Price = summary.Price,
            SalePrice = summary.SalePrice,
            ServiceType = summary.ServiceType,
            ImageUrl = summary.ImageUrl,
            HasActiveOffer = summary.HasActiveOffer,
            OfflinePaymentPercent = service.OfflinePaymentPercent.HasValue
                ? (double)service.OfflinePaymentPercent.Value
                : null,
            ImageUrls = imageUrls,
            ActiveOffers = GetActiveOffers(service, now)
        };
    }

    private static ServiceSubscriptionSummaryDto MapSummary(ServiceSubscription service, DateTime now)
    {
        var activeOffers = GetActiveOfferEntities(service, now);
        var price = (double)service.Price;
        var salePrice = activeOffers.Count > 0 ? CalculateSalePrice(service.Price, activeOffers[0]) : (double?)null;

        return new ServiceSubscriptionSummaryDto
        {
            Id = service.Id,
            Title = service.Title,
            TitleAr = service.TitleAr,
            Description = service.Description,
            DescriptionAr = service.DescriptionAr,
            Price = price,
            SalePrice = salePrice,
            ServiceType = service.ServiceType.ToString(),
            ImageUrl = ResolvePrimaryImage(service),
            HasActiveOffer = activeOffers.Count > 0
        };
    }

    private static List<ServiceOffer> GetActiveOfferEntities(ServiceSubscription service, DateTime now) =>
        service.Offers
            .Where(o => o.IsActive && o.StartDate <= now && o.EndDate >= now)
            .OrderByDescending(o => o.DiscountValue)
            .ToList();

    private static IReadOnlyList<ServiceOfferDto> GetActiveOffers(ServiceSubscription service, DateTime now) =>
        GetActiveOfferEntities(service, now)
            .Select(o => new ServiceOfferDto
            {
                Id = o.Id,
                DiscountType = o.DiscountType.ToString(),
                DiscountValue = (double)o.DiscountValue,
                StartDate = o.StartDate,
                EndDate = o.EndDate
            })
            .ToList();

    private static double CalculateSalePrice(decimal price, ServiceOffer offer)
    {
        var discount = offer.DiscountType == ServiceDiscountType.Percentage
            ? price * (offer.DiscountValue / 100m)
            : offer.DiscountValue;

        var final = price - discount;
        return final < 0 ? 0 : (double)final;
    }

    private static string? ResolvePrimaryImage(ServiceSubscription service)
    {
        var fromGallery = service.Images
            .OrderBy(i => i.DisplayOrder)
            .Select(i => i.ImageUrl)
            .FirstOrDefault(url => !string.IsNullOrWhiteSpace(url));

        return !string.IsNullOrWhiteSpace(fromGallery) ? fromGallery : service.ImageUrl;
    }

    private static IReadOnlyList<string> BuildImageUrls(ServiceSubscription service)
    {
        var urls = service.Images
            .OrderBy(i => i.DisplayOrder)
            .Select(i => i.ImageUrl)
            .Where(url => !string.IsNullOrWhiteSpace(url))
            .ToList();

        if (urls.Count == 0 && !string.IsNullOrWhiteSpace(service.ImageUrl))
            urls.Add(service.ImageUrl);

        return urls;
    }
}
