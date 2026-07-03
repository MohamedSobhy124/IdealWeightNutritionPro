namespace IdealWeightNutrition.Contracts.Admin;

public enum PromoDiscountType
{
    Percentage = 1,
    FixedAmount = 2
}

public sealed class AdminPromoCodeListItemDto
{
    public required int Id { get; init; }
    public required string Code { get; init; }
    public required string Description { get; init; }
    public required PromoDiscountType DiscountType { get; init; }
    public required decimal DiscountValue { get; init; }
    public required DateTime StartDate { get; init; }
    public required DateTime EndDate { get; init; }
    public required int TimesUsed { get; init; }
    public int? UsageLimit { get; init; }
    public required bool IsActive { get; init; }
}

public sealed class AdminPromoCodeDetailDto
{
    public required int Id { get; init; }
    public required string Code { get; init; }
    public required string Description { get; init; }
    public required PromoDiscountType DiscountType { get; init; }
    public required decimal DiscountValue { get; init; }
    public decimal? MinimumOrderAmount { get; init; }
    public decimal? MaximumDiscountAmount { get; init; }
    public required DateTime StartDate { get; init; }
    public required DateTime EndDate { get; init; }
    public int? UsageLimit { get; init; }
    public required int TimesUsed { get; init; }
    public int? UsageLimitPerUser { get; init; }
    public required bool IsActive { get; init; }
    public required bool ExcludeDiscountedItems { get; init; }
    public required bool ExcludeAllServices { get; init; }
}

public sealed class UpsertAdminPromoCodeRequest
{
    public required string Code { get; init; }
    public required string Description { get; init; }
    public PromoDiscountType DiscountType { get; init; }
    public decimal DiscountValue { get; init; }
    public decimal? MinimumOrderAmount { get; init; }
    public decimal? MaximumDiscountAmount { get; init; }
    public DateTime StartDate { get; init; }
    public DateTime EndDate { get; init; }
    public int? UsageLimit { get; init; }
    public int? UsageLimitPerUser { get; init; }
    public bool IsActive { get; init; } = true;
    public bool ExcludeDiscountedItems { get; init; }
    public bool ExcludeAllServices { get; init; } = true;
}

public sealed class PromoCodeExclusionsDto
{
    public required IReadOnlyList<PromoExcludedProductDto> Products { get; init; }
    public required IReadOnlyList<PromoExcludedComboOfferDto> ComboOffers { get; init; }
    public required IReadOnlyList<PromoExcludedServiceDto> Services { get; init; }
}

public sealed class PromoExcludedProductDto
{
    public required int Id { get; init; }
    public required int ProductId { get; init; }
    public required string Title { get; init; }
    public string? TitleAr { get; init; }
}

public sealed class PromoExcludedComboOfferDto
{
    public required int Id { get; init; }
    public required int ComboOfferId { get; init; }
    public required string Name { get; init; }
    public string? NameAr { get; init; }
}

public sealed class PromoExcludedServiceDto
{
    public required int Id { get; init; }
    public required int ServiceSubscriptionId { get; init; }
    public required string Title { get; init; }
    public string? TitleAr { get; init; }
}
