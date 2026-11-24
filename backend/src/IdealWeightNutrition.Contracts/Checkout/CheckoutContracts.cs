namespace IdealWeightNutrition.Contracts.Checkout;

public sealed class CityDto
{
    public required int Id { get; init; }
    public required string Name { get; init; }
    public string? NameAr { get; init; }
    public required string Emirate { get; init; }
    public required double DeliveryCharge { get; init; }
}

public sealed class RemoteAreaDto
{
    public required int Id { get; init; }
    public required int CityId { get; init; }
    public required string Name { get; init; }
    public string? NameAr { get; init; }
    public required double DeliveryCharge { get; init; }
}

public sealed class ShippingQuoteRequest
{
    public int CityId { get; init; }
    public int? RemoteAreaId { get; init; }
}

public sealed class ShippingQuoteResponse
{
    public required double Subtotal { get; init; }
    public double Discount { get; init; }
    public required double Shipping { get; init; }
    public required double Total { get; init; }
    public required string CityName { get; init; }
    public string? AreaName { get; init; }

    /// <summary>Delivery charge before any free-delivery rule (city or remote-area charge).</summary>
    public double BaseShipping { get; init; }
    /// <summary>The order currently ships for free.</summary>
    public bool IsFreeDelivery { get; init; }
    /// <summary>Every item in the cart allows free delivery, so a threshold applies.</summary>
    public bool FreeDeliveryAvailable { get; init; }
    /// <summary>Subtotal (before promo discount) needed for free delivery; null when not available.</summary>
    public double? FreeDeliveryThreshold { get; init; }
    /// <summary>How much more subtotal unlocks free delivery; 0 when already free or not available.</summary>
    public double AmountRemainingForFreeDelivery { get; init; }
}

public sealed class CheckoutOtpRequest
{
    public required string Email { get; init; }
}

public sealed class VerifyCheckoutOtpRequest
{
    public required string Email { get; init; }
    public required string Otp { get; init; }
}

public sealed class CreateOrderRequest
{
    public required string Name { get; init; }
    public required string Email { get; init; }
    public required string PhoneNumber { get; init; }
    public required string StreetAddress { get; init; }
    public required int CityId { get; init; }
    public int? RemoteAreaId { get; init; }
    public string State { get; init; } = "UAE";
    public string PostalCode { get; init; } = "00000";
    public string? Otp { get; init; }
    public required string PaymentMethod { get; init; }
    public bool CreateAccountForGuest { get; init; }
}

public sealed class CreateOrderResponse
{
    public required int OrderId { get; init; }
    public required string OrderStatus { get; init; }
    public required string PaymentStatus { get; init; }
    public required double OrderTotal { get; init; }
    public string? PaymentMethod { get; init; }
    public bool RequiresPaymentAction { get; init; }
    public string? PaymentSessionId { get; init; }
    public string? PaymentRedirectUrl { get; init; }
    public bool AccountCreated { get; init; }
    public bool AccountLinked { get; init; }
}
