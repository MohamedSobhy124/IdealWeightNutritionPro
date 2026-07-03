using IdealWeightNutrition.Application.Abstractions;
using IdealWeightNutrition.Contracts.Admin;
using IdealWeightNutrition.Domain.Promotions;
using IdealWeightNutrition.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace IdealWeightNutrition.Infrastructure.Services;

internal sealed class AdminPromoCodeService : IAdminPromoCodeService
{
    private readonly AppDbContext _db;
    private readonly IDateTimeProvider _clock;

    public AdminPromoCodeService(AppDbContext db, IDateTimeProvider clock)
    {
        _db = db;
        _clock = clock;
    }

    public async Task<IReadOnlyList<AdminPromoCodeListItemDto>> ListAsync(CancellationToken cancellationToken = default) =>
        await _db.PromoCodes
            .AsNoTracking()
            .OrderByDescending(p => p.CreatedDate)
            .Select(p => new AdminPromoCodeListItemDto
            {
                Id = p.Id,
                Code = p.Code,
                Description = p.Description,
                DiscountType = (PromoDiscountType)p.DiscountType,
                DiscountValue = p.DiscountValue,
                StartDate = p.StartDate,
                EndDate = p.EndDate,
                TimesUsed = p.TimesUsed,
                UsageLimit = p.UsageLimit,
                IsActive = p.IsActive
            })
            .ToListAsync(cancellationToken);

    public async Task<AdminPromoCodeDetailDto?> GetAsync(int id, CancellationToken cancellationToken = default)
    {
        var promo = await _db.PromoCodes.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
        return promo is null ? null : MapDetail(promo);
    }

    public async Task<AdminPromoCodeDetailDto> CreateAsync(
        UpsertAdminPromoCodeRequest request,
        string? createdBy,
        CancellationToken cancellationToken = default)
    {
        ValidateRequest(request);

        var code = request.Code.Trim();
        var exists = await _db.PromoCodes.AnyAsync(
            p => p.Code.ToLower() == code.ToLower(),
            cancellationToken);
        if (exists)
            throw new InvalidOperationException("This promo code already exists.");

        var promo = new PromoCode
        {
            Code = code,
            Description = request.Description.Trim(),
            DiscountType = (DiscountType)request.DiscountType,
            DiscountValue = request.DiscountValue,
            MinimumOrderAmount = request.MinimumOrderAmount,
            MaximumDiscountAmount = request.MaximumDiscountAmount,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            UsageLimit = request.UsageLimit,
            UsageLimitPerUser = request.UsageLimitPerUser,
            IsActive = request.IsActive,
            ExcludeDiscountedItems = request.ExcludeDiscountedItems,
            ExcludeAllServices = request.ExcludeAllServices,
            TimesUsed = 0,
            CreatedDate = _clock.Now,
            CreatedBy = createdBy
        };

        _db.PromoCodes.Add(promo);
        await _db.SaveChangesAsync(cancellationToken);
        return MapDetail(promo);
    }

    public async Task<AdminPromoCodeDetailDto> UpdateAsync(
        int id,
        UpsertAdminPromoCodeRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidateRequest(request);

        var promo = await _db.PromoCodes.FirstOrDefaultAsync(p => p.Id == id, cancellationToken)
            ?? throw new InvalidOperationException("Promo code not found.");

        var code = request.Code.Trim();
        var duplicate = await _db.PromoCodes.AnyAsync(
            p => p.Id != id && p.Code.ToLower() == code.ToLower(),
            cancellationToken);
        if (duplicate)
            throw new InvalidOperationException("This promo code already exists.");

        promo.Code = code;
        promo.Description = request.Description.Trim();
        promo.DiscountType = (DiscountType)request.DiscountType;
        promo.DiscountValue = request.DiscountValue;
        promo.MinimumOrderAmount = request.MinimumOrderAmount;
        promo.MaximumDiscountAmount = request.MaximumDiscountAmount;
        promo.StartDate = request.StartDate;
        promo.EndDate = request.EndDate;
        promo.UsageLimit = request.UsageLimit;
        promo.UsageLimitPerUser = request.UsageLimitPerUser;
        promo.IsActive = request.IsActive;
        promo.ExcludeDiscountedItems = request.ExcludeDiscountedItems;
        promo.ExcludeAllServices = request.ExcludeAllServices;

        await _db.SaveChangesAsync(cancellationToken);
        return MapDetail(promo);
    }

    public async Task ToggleActiveAsync(int id, CancellationToken cancellationToken = default)
    {
        var promo = await _db.PromoCodes.FirstOrDefaultAsync(p => p.Id == id, cancellationToken)
            ?? throw new InvalidOperationException("Promo code not found.");

        promo.IsActive = !promo.IsActive;
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var promo = await _db.PromoCodes
            .Include(p => p.ExcludedProducts)
            .Include(p => p.ExcludedComboOffers)
            .Include(p => p.ExcludedServiceSubscriptions)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken)
            ?? throw new InvalidOperationException("Promo code not found.");

        if (promo.TimesUsed > 0)
            throw new InvalidOperationException("Cannot delete a promo code that has been used.");

        _db.PromoCodes.Remove(promo);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<PromoCodeExclusionsDto> GetExclusionsAsync(int promoId, CancellationToken cancellationToken = default)
    {
        var promo = await _db.PromoCodes.AsNoTracking()
            .Include(p => p.ExcludedProducts)
            .Include(p => p.ExcludedComboOffers)
            .Include(p => p.ExcludedServiceSubscriptions)
            .FirstOrDefaultAsync(p => p.Id == promoId, cancellationToken)
            ?? throw new InvalidOperationException("Promo code not found.");

        var productIds = promo.ExcludedProducts.Select(e => e.ProductId).ToList();
        var products = productIds.Count == 0
            ? new Dictionary<int, Domain.Catalogue.Product>()
            : await _db.Products.AsNoTracking()
                .Where(p => productIds.Contains(p.Id))
                .ToDictionaryAsync(p => p.Id, cancellationToken);

        var comboIds = promo.ExcludedComboOffers.Select(e => e.ComboOfferId).ToList();
        var combos = comboIds.Count == 0
            ? new Dictionary<int, Domain.Promotions.ComboOffer>()
            : await _db.ComboOffers.AsNoTracking()
                .Where(c => comboIds.Contains(c.Id))
                .ToDictionaryAsync(c => c.Id, cancellationToken);

        var serviceIds = promo.ExcludedServiceSubscriptions.Select(e => e.ServiceSubscriptionId).ToList();
        var services = serviceIds.Count == 0
            ? new Dictionary<int, Domain.Services.ServiceSubscription>()
            : await _db.ServiceSubscriptions.AsNoTracking()
                .Where(s => serviceIds.Contains(s.Id))
                .ToDictionaryAsync(s => s.Id, cancellationToken);

        return new PromoCodeExclusionsDto
        {
            Products = promo.ExcludedProducts.Select(e =>
            {
                products.TryGetValue(e.ProductId, out var product);
                return new PromoExcludedProductDto
                {
                    Id = e.Id,
                    ProductId = e.ProductId,
                    Title = product?.Title ?? $"Product #{e.ProductId}",
                    TitleAr = product?.TitleAr
                };
            }).ToList(),
            ComboOffers = promo.ExcludedComboOffers.Select(e =>
            {
                combos.TryGetValue(e.ComboOfferId, out var combo);
                return new PromoExcludedComboOfferDto
                {
                    Id = e.Id,
                    ComboOfferId = e.ComboOfferId,
                    Name = combo?.Name ?? $"Combo #{e.ComboOfferId}",
                    NameAr = combo?.NameAr
                };
            }).ToList(),
            Services = promo.ExcludedServiceSubscriptions.Select(e =>
            {
                services.TryGetValue(e.ServiceSubscriptionId, out var service);
                return new PromoExcludedServiceDto
                {
                    Id = e.Id,
                    ServiceSubscriptionId = e.ServiceSubscriptionId,
                    Title = service?.Title ?? $"Service #{e.ServiceSubscriptionId}",
                    TitleAr = service?.TitleAr
                };
            }).ToList()
        };
    }

    public async Task AddExcludedProductAsync(int promoId, int productId, CancellationToken cancellationToken = default)
    {
        await EnsurePromoExistsAsync(promoId, cancellationToken);

        var productExists = await _db.Products.AnyAsync(p => p.Id == productId && !p.IsDeleted, cancellationToken);
        if (!productExists)
            throw new InvalidOperationException("Product not found.");

        var exists = await _db.Set<PromoCodeExcludedProduct>()
            .AnyAsync(e => e.PromoCodeId == promoId && e.ProductId == productId, cancellationToken);
        if (exists)
            throw new InvalidOperationException("Product is already excluded.");

        _db.Set<PromoCodeExcludedProduct>().Add(new PromoCodeExcludedProduct
        {
            PromoCodeId = promoId,
            ProductId = productId
        });
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task RemoveExcludedProductAsync(int exclusionId, CancellationToken cancellationToken = default)
    {
        var row = await _db.Set<PromoCodeExcludedProduct>().FirstOrDefaultAsync(e => e.Id == exclusionId, cancellationToken)
            ?? throw new InvalidOperationException("Excluded product not found.");

        _db.Set<PromoCodeExcludedProduct>().Remove(row);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task AddExcludedComboOfferAsync(int promoId, int comboOfferId, CancellationToken cancellationToken = default)
    {
        await EnsurePromoExistsAsync(promoId, cancellationToken);

        var comboExists = await _db.ComboOffers.AnyAsync(c => c.Id == comboOfferId && !c.IsDeleted, cancellationToken);
        if (!comboExists)
            throw new InvalidOperationException("Combo offer not found.");

        var exists = await _db.Set<PromoCodeExcludedComboOffer>()
            .AnyAsync(e => e.PromoCodeId == promoId && e.ComboOfferId == comboOfferId, cancellationToken);
        if (exists)
            throw new InvalidOperationException("Combo offer is already excluded.");

        _db.Set<PromoCodeExcludedComboOffer>().Add(new PromoCodeExcludedComboOffer
        {
            PromoCodeId = promoId,
            ComboOfferId = comboOfferId
        });
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task RemoveExcludedComboOfferAsync(int exclusionId, CancellationToken cancellationToken = default)
    {
        var row = await _db.Set<PromoCodeExcludedComboOffer>().FirstOrDefaultAsync(e => e.Id == exclusionId, cancellationToken)
            ?? throw new InvalidOperationException("Excluded combo offer not found.");

        _db.Set<PromoCodeExcludedComboOffer>().Remove(row);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task AddExcludedServiceAsync(int promoId, int serviceSubscriptionId, CancellationToken cancellationToken = default)
    {
        await EnsurePromoExistsAsync(promoId, cancellationToken);

        var serviceExists = await _db.ServiceSubscriptions.AnyAsync(s => s.Id == serviceSubscriptionId && s.IsActive, cancellationToken);
        if (!serviceExists)
            throw new InvalidOperationException("Service not found.");

        var exists = await _db.Set<PromoCodeExcludedServiceSubscription>()
            .AnyAsync(e => e.PromoCodeId == promoId && e.ServiceSubscriptionId == serviceSubscriptionId, cancellationToken);
        if (exists)
            throw new InvalidOperationException("Service is already excluded.");

        _db.Set<PromoCodeExcludedServiceSubscription>().Add(new PromoCodeExcludedServiceSubscription
        {
            PromoCodeId = promoId,
            ServiceSubscriptionId = serviceSubscriptionId
        });
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task RemoveExcludedServiceAsync(int exclusionId, CancellationToken cancellationToken = default)
    {
        var row = await _db.Set<PromoCodeExcludedServiceSubscription>().FirstOrDefaultAsync(e => e.Id == exclusionId, cancellationToken)
            ?? throw new InvalidOperationException("Excluded service not found.");

        _db.Set<PromoCodeExcludedServiceSubscription>().Remove(row);
        await _db.SaveChangesAsync(cancellationToken);
    }

    private async Task EnsurePromoExistsAsync(int promoId, CancellationToken cancellationToken)
    {
        if (!await _db.PromoCodes.AnyAsync(p => p.Id == promoId, cancellationToken))
            throw new InvalidOperationException("Promo code not found.");
    }

    private static void ValidateRequest(UpsertAdminPromoCodeRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Code))
            throw new InvalidOperationException("Promo code is required.");
        if (string.IsNullOrWhiteSpace(request.Description))
            throw new InvalidOperationException("Description is required.");
        if (request.DiscountValue <= 0)
            throw new InvalidOperationException("Discount value must be greater than 0.");
        if (request.EndDate <= request.StartDate)
            throw new InvalidOperationException("End date must be after start date.");
        if (request.DiscountType == PromoDiscountType.Percentage && request.DiscountValue > 100)
            throw new InvalidOperationException("Percentage discount cannot exceed 100.");
    }

    private static AdminPromoCodeDetailDto MapDetail(PromoCode promo) => new()
    {
        Id = promo.Id,
        Code = promo.Code,
        Description = promo.Description,
        DiscountType = (PromoDiscountType)promo.DiscountType,
        DiscountValue = promo.DiscountValue,
        MinimumOrderAmount = promo.MinimumOrderAmount,
        MaximumDiscountAmount = promo.MaximumDiscountAmount,
        StartDate = promo.StartDate,
        EndDate = promo.EndDate,
        UsageLimit = promo.UsageLimit,
        TimesUsed = promo.TimesUsed,
        UsageLimitPerUser = promo.UsageLimitPerUser,
        IsActive = promo.IsActive,
        ExcludeDiscountedItems = promo.ExcludeDiscountedItems,
        ExcludeAllServices = promo.ExcludeAllServices
    };
}
