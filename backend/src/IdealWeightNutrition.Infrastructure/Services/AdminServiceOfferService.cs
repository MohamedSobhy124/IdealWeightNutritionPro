using IdealWeightNutrition.Application.Abstractions;
using IdealWeightNutrition.Contracts.Admin;
using IdealWeightNutrition.Domain.Services;
using IdealWeightNutrition.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace IdealWeightNutrition.Infrastructure.Services;

internal sealed class AdminServiceOfferService : IAdminServiceOfferService
{
    private readonly AppDbContext _db;
    private readonly IDateTimeProvider _clock;

    public AdminServiceOfferService(AppDbContext db, IDateTimeProvider clock)
    {
        _db = db;
        _clock = clock;
    }

    public async Task<IReadOnlyList<AdminServiceOfferListItemDto>> ListAsync(
        int? serviceSubscriptionId,
        CancellationToken cancellationToken = default)
    {
        var query = _db.Set<ServiceOffer>().AsNoTracking();
        if (serviceSubscriptionId is > 0)
            query = query.Where(o => o.ServiceSubscriptionId == serviceSubscriptionId);

        var offers = await query
            .OrderByDescending(o => o.CreatedDate)
            .ToListAsync(cancellationToken);

        return await MapListAsync(offers, cancellationToken);
    }

    public async Task<AdminServiceOfferDetailDto?> GetAsync(int id, CancellationToken cancellationToken = default)
    {
        var offer = await _db.Set<ServiceOffer>().AsNoTracking()
            .FirstOrDefaultAsync(o => o.Id == id, cancellationToken);

        if (offer is null)
            return null;

        var list = await MapListAsync([offer], cancellationToken);
        var item = list[0];
        return new AdminServiceOfferDetailDto
        {
            Id = item.Id,
            ServiceSubscriptionId = item.ServiceSubscriptionId,
            ServiceTitle = item.ServiceTitle,
            PromoCodeId = item.PromoCodeId,
            PromoCode = item.PromoCode,
            DiscountType = item.DiscountType,
            DiscountValue = item.DiscountValue,
            StartDate = item.StartDate,
            EndDate = item.EndDate,
            IsActive = item.IsActive,
            CreatedDate = offer.CreatedDate
        };
    }

    public async Task<AdminServiceOfferDetailDto> CreateAsync(
        UpsertAdminServiceOfferRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidateRequest(request);
        await EnsureServiceExistsAsync(request.ServiceSubscriptionId, cancellationToken);
        await ValidatePromoCodeAsync(request.PromoCodeId, cancellationToken);

        var offer = new ServiceOffer
        {
            ServiceSubscriptionId = request.ServiceSubscriptionId,
            PromoCodeId = request.PromoCodeId,
            DiscountType = MapDiscountType(request.DiscountType),
            DiscountValue = (decimal)request.DiscountValue,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            IsActive = request.IsActive,
            CreatedDate = _clock.Now
        };

        _db.Set<ServiceOffer>().Add(offer);
        await _db.SaveChangesAsync(cancellationToken);

        return (await GetAsync(offer.Id, cancellationToken))!;
    }

    public async Task<AdminServiceOfferDetailDto> UpdateAsync(
        int id,
        UpsertAdminServiceOfferRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidateRequest(request);
        await EnsureServiceExistsAsync(request.ServiceSubscriptionId, cancellationToken);
        await ValidatePromoCodeAsync(request.PromoCodeId, cancellationToken);

        var offer = await _db.Set<ServiceOffer>().FirstOrDefaultAsync(o => o.Id == id, cancellationToken)
            ?? throw new InvalidOperationException("Service offer not found.");

        offer.ServiceSubscriptionId = request.ServiceSubscriptionId;
        offer.PromoCodeId = request.PromoCodeId;
        offer.DiscountType = MapDiscountType(request.DiscountType);
        offer.DiscountValue = (decimal)request.DiscountValue;
        offer.StartDate = request.StartDate;
        offer.EndDate = request.EndDate;
        offer.IsActive = request.IsActive;

        await _db.SaveChangesAsync(cancellationToken);
        return (await GetAsync(id, cancellationToken))!;
    }

    public async Task ToggleActiveAsync(int id, CancellationToken cancellationToken = default)
    {
        var offer = await _db.Set<ServiceOffer>().FirstOrDefaultAsync(o => o.Id == id, cancellationToken)
            ?? throw new InvalidOperationException("Service offer not found.");

        offer.IsActive = !offer.IsActive;
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var offer = await _db.Set<ServiceOffer>().FirstOrDefaultAsync(o => o.Id == id, cancellationToken)
            ?? throw new InvalidOperationException("Service offer not found.");

        _db.Set<ServiceOffer>().Remove(offer);
        await _db.SaveChangesAsync(cancellationToken);
    }

    private async Task<IReadOnlyList<AdminServiceOfferListItemDto>> MapListAsync(
        IReadOnlyList<ServiceOffer> offers,
        CancellationToken cancellationToken)
    {
        if (offers.Count == 0)
            return [];

        var serviceIds = offers.Select(o => o.ServiceSubscriptionId).Distinct().ToList();
        var promoIds = offers.Where(o => o.PromoCodeId is > 0).Select(o => o.PromoCodeId!.Value).Distinct().ToList();

        var services = await _db.ServiceSubscriptions.AsNoTracking()
            .Where(s => serviceIds.Contains(s.Id))
            .ToDictionaryAsync(s => s.Id, s => s.Title, cancellationToken);

        var promos = promoIds.Count == 0
            ? new Dictionary<int, string>()
            : await _db.PromoCodes.AsNoTracking()
                .Where(p => promoIds.Contains(p.Id))
                .ToDictionaryAsync(p => p.Id, p => p.Code, cancellationToken);

        return offers.Select(o => new AdminServiceOfferListItemDto
        {
            Id = o.Id,
            ServiceSubscriptionId = o.ServiceSubscriptionId,
            ServiceTitle = services.GetValueOrDefault(o.ServiceSubscriptionId, $"Service #{o.ServiceSubscriptionId}"),
            PromoCodeId = o.PromoCodeId,
            PromoCode = o.PromoCodeId is > 0 ? promos.GetValueOrDefault(o.PromoCodeId.Value) : null,
            DiscountType = MapDiscountType(o.DiscountType),
            DiscountValue = (double)o.DiscountValue,
            StartDate = o.StartDate,
            EndDate = o.EndDate,
            IsActive = o.IsActive
        }).ToList();
    }

    private async Task EnsureServiceExistsAsync(int serviceId, CancellationToken cancellationToken)
    {
        var exists = await _db.ServiceSubscriptions.AnyAsync(s => s.Id == serviceId, cancellationToken);
        if (!exists)
            throw new InvalidOperationException("Service not found.");
    }

    private async Task ValidatePromoCodeAsync(int? promoCodeId, CancellationToken cancellationToken)
    {
        if (promoCodeId is not > 0)
            return;

        var exists = await _db.PromoCodes.AnyAsync(p => p.Id == promoCodeId, cancellationToken);
        if (!exists)
            throw new InvalidOperationException("Promo code not found.");
    }

    private static void ValidateRequest(UpsertAdminServiceOfferRequest request)
    {
        if (request.ServiceSubscriptionId <= 0)
            throw new InvalidOperationException("Service is required.");
        if (request.DiscountValue <= 0)
            throw new InvalidOperationException("Discount value must be greater than 0.");
        if (request.EndDate <= request.StartDate)
            throw new InvalidOperationException("End date must be after start date.");
        if (request.DiscountType == AdminServiceDiscountType.Percentage && request.DiscountValue > 100)
            throw new InvalidOperationException("Percentage discount cannot exceed 100%.");
    }

    private static ServiceDiscountType MapDiscountType(AdminServiceDiscountType type) =>
        type == AdminServiceDiscountType.FixedAmount
            ? ServiceDiscountType.FixedAmount
            : ServiceDiscountType.Percentage;

    private static AdminServiceDiscountType MapDiscountType(ServiceDiscountType type) =>
        type == ServiceDiscountType.FixedAmount
            ? AdminServiceDiscountType.FixedAmount
            : AdminServiceDiscountType.Percentage;
}
