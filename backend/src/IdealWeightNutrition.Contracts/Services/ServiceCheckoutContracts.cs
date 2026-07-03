namespace IdealWeightNutrition.Contracts.Services;

public sealed class ServiceCheckoutQuoteRequest
{
    public required int ServiceId { get; init; }
    public int? OfferId { get; init; }
    public string? PromoCode { get; init; }
    public double? CustomAmount { get; init; }
}

public sealed class ServiceCheckoutQuoteResponse
{
    public required int ServiceId { get; init; }
    public required string ServiceTitle { get; init; }
    public string? ServiceTitleAr { get; init; }
    public required double ListPrice { get; init; }
    public double DiscountAmount { get; init; }
    public required double TotalAmount { get; init; }
    public required double AmountToPay { get; init; }
    public double? MinPaymentAmount { get; init; }
    public bool IsFree { get; init; }
    public required string ServiceType { get; init; }
    public int? AppliedOfferId { get; init; }
    public string? AppliedPromoCode { get; init; }
    public string? PromoMessage { get; init; }
}

public sealed class CreateServicePurchaseRequest
{
    public required string Name { get; init; }
    public required string Email { get; init; }
    public required string PhoneNumber { get; init; }
    public int? OfferId { get; init; }
    public string? PromoCode { get; init; }
    public double? CustomAmount { get; init; }
    public string? Otp { get; init; }
    public required string PaymentMethod { get; init; }
    public bool CreateAccountForGuest { get; init; }
}

public sealed class CreateServicePurchaseResponse
{
    public required int PurchaseId { get; init; }
    public required string PaymentStatus { get; init; }
    public required double AmountPaid { get; init; }
    public string? PaymentMethod { get; init; }
    public bool RequiresPaymentAction { get; init; }
    public string? PaymentSessionId { get; init; }
    public string? PaymentRedirectUrl { get; init; }
    public bool IsPaid { get; init; }
    public bool AccountCreated { get; init; }
    public bool AccountLinked { get; init; }
}

public sealed class CompleteServicePaymentResponse
{
    public required int PurchaseId { get; init; }
    public required string PaymentStatus { get; init; }
    public required bool IsPaid { get; init; }
    public string? Message { get; init; }
}
