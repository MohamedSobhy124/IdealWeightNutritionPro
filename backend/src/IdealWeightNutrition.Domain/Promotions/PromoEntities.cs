namespace IdealWeightNutrition.Domain.Promotions;

public enum DiscountType
{
    Percentage = 1,
    FixedAmount = 2
}

public sealed class PromoCode
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DiscountType DiscountType { get; set; }
    public decimal DiscountValue { get; set; }
    public decimal? MinimumOrderAmount { get; set; }
    public decimal? MaximumDiscountAmount { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int? UsageLimit { get; set; }
    public int TimesUsed { get; set; }
    public int? UsageLimitPerUser { get; set; }
    public bool IsActive { get; set; }
    public bool ExcludeDiscountedItems { get; set; }
    public bool ExcludeAllServices { get; set; } = true;
    public DateTime CreatedDate { get; set; }
    public string? CreatedBy { get; set; }
    public ICollection<PromoCodeExcludedProduct> ExcludedProducts { get; set; } = new List<PromoCodeExcludedProduct>();
    public ICollection<PromoCodeExcludedComboOffer> ExcludedComboOffers { get; set; } = new List<PromoCodeExcludedComboOffer>();
    public ICollection<PromoCodeExcludedServiceSubscription> ExcludedServiceSubscriptions { get; set; } =
        new List<PromoCodeExcludedServiceSubscription>();
}

public sealed class PromoCodeExcludedProduct
{
    public int Id { get; set; }
    public int PromoCodeId { get; set; }
    public int ProductId { get; set; }
}

public sealed class PromoCodeExcludedComboOffer
{
    public int Id { get; set; }
    public int PromoCodeId { get; set; }
    public int ComboOfferId { get; set; }
}

public sealed class PromoCodeExcludedServiceSubscription
{
    public int Id { get; set; }
    public int PromoCodeId { get; set; }
    public int ServiceSubscriptionId { get; set; }
}

public sealed class PromoCodeUsage
{
    public int Id { get; set; }
    public int PromoCodeId { get; set; }
    public string UserId { get; set; } = string.Empty;
    public DateTime UsedDate { get; set; }
    public int OrderId { get; set; }
}
