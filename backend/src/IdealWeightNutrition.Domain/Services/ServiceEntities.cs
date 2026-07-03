namespace IdealWeightNutrition.Domain.Services;

public enum ServiceType
{
    Online = 1,
    Offline = 2
}

public enum ServiceDiscountType
{
    Percentage = 1,
    FixedAmount = 2
}

public sealed class ServiceSubscription
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? TitleAr { get; set; }
    public string? Description { get; set; }
    public string? DescriptionAr { get; set; }
    public decimal Price { get; set; }
    public ServiceType ServiceType { get; set; }
    public decimal? OfflinePaymentPercent { get; set; }
    public string? ImageUrl { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime? UpdatedDate { get; set; }
    public int DisplayOrder { get; set; }
    public ICollection<ServiceImage> Images { get; set; } = new List<ServiceImage>();
    public ICollection<ServiceOffer> Offers { get; set; } = new List<ServiceOffer>();
}

public sealed class ServiceImage
{
    public int Id { get; set; }
    public int ServiceSubscriptionId { get; set; }
    public string ImageUrl { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }
    public ServiceSubscription? ServiceSubscription { get; set; }
}

public sealed class ServiceOffer
{
    public int Id { get; set; }
    public int ServiceSubscriptionId { get; set; }
    public int? PromoCodeId { get; set; }
    public ServiceDiscountType DiscountType { get; set; }
    public decimal DiscountValue { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedDate { get; set; }
    public string? CreatedBy { get; set; }
    public ServiceSubscription? ServiceSubscription { get; set; }
}

public sealed class ServicePurchase
{
    public int Id { get; set; }
    public int ServiceSubscriptionId { get; set; }
    public string? ApplicationUserId { get; set; }
    public string? GuestEmail { get; set; }
    public string? GuestName { get; set; }
    public string? GuestPhone { get; set; }
    public decimal AmountPaid { get; set; }
    public decimal TotalAmount { get; set; }
    public string PaymentStatus { get; set; } = "Pending";
    public string? PaymentIntentId { get; set; }
    public string? SessionId { get; set; }
    public int? ServiceOfferId { get; set; }
    public decimal DiscountAmount { get; set; }
    public DateTime PurchaseDate { get; set; }
    public string Status { get; set; } = "Active";
    public ServiceSubscription? ServiceSubscription { get; set; }
}
