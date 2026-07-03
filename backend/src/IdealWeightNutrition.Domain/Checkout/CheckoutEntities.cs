namespace IdealWeightNutrition.Domain.Checkout;

public sealed class City
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? NameAr { get; set; }
    public string Emirate { get; set; } = string.Empty;
    public string? EmirateAr { get; set; }
    public double DeliveryCharge { get; set; }
    public bool IsActive { get; set; }
    public int DisplayOrder { get; set; }
}

public sealed class RemoteArea
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? NameAr { get; set; }
    public int CityId { get; set; }
    public double DeliveryCharge { get; set; }
    public bool IsActive { get; set; }
    public int DisplayOrder { get; set; }
    public City? City { get; set; }
}

public sealed class OrderHeader
{
    public int Id { get; set; }
    public string? ApplicationUserId { get; set; }
    public string? Email { get; set; }
    public bool IsGuestOrder { get; set; }
    public DateTime OrderDate { get; set; }
    public DateTime ShippingDate { get; set; }
    public double OrderTotal { get; set; }
    public string? OrderStatus { get; set; }
    public string? PaymentStatus { get; set; }
    public string? TrackingNumber { get; set; }
    public string? Carrier { get; set; }
    public string? PaymentMethod { get; set; }
    public string? SessionId { get; set; }
    public string? PaymentIntentId { get; set; }
    public DateTime PaymentDate { get; set; }
    public DateTime PaymentDueDate { get; set; }
    public string PhoneNumber { get; set; } = string.Empty;
    public string StreetAddress { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string? Area { get; set; }
    public string State { get; set; } = string.Empty;
    public string PostalCode { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public double? OrderSubtotal { get; set; }
    public double? DiscountAmount { get; set; }
    public int? PromoCodeId { get; set; }
    public string? PromoCodeText { get; set; }
    public ICollection<OrderDetail> Details { get; set; } = new List<OrderDetail>();
}

public sealed class OrderDetail
{
    public int Id { get; set; }
    public int OrderHeaderId { get; set; }
    public int ProductId { get; set; }
    public int Count { get; set; }
    public double Price { get; set; }
    public int? ProductVariantId { get; set; }
    public int? FlashSaleItemId { get; set; }
    public int? ComboOfferId { get; set; }
    public decimal? PromoCodeDiscountAmount { get; set; }
}

public sealed class OrderAuditLog
{
    public int Id { get; set; }
    public int OrderHeaderId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string? ActionDetails { get; set; }
    public string? PerformedByUserId { get; set; }
    public string? PerformedByUserEmail { get; set; }
    public string? OldOrderStatus { get; set; }
    public string? NewOrderStatus { get; set; }
    public string? OldPaymentStatus { get; set; }
    public string? NewPaymentStatus { get; set; }
    public DateTime ActionDate { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime CreatedDate { get; set; }
    public bool IsDeleted { get; set; }
    public string? ModifiedBy { get; set; }
    public DateTime? ModifiedDate { get; set; }
}
