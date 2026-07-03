using System.Text.Json.Serialization;

namespace IdealWeightNutrition.Contracts.Checkout;

public sealed class PaymentMethodOptionDto
{
    public required string Id { get; init; }
    public required string Label { get; init; }
    public required bool Available { get; init; }
    public string? UnavailableReason { get; init; }
    public string? UnavailableReasonCode { get; init; }
    public double? MinimumAmount { get; init; }
}

public sealed class PaymentMethodsResponse
{
    public required IReadOnlyList<PaymentMethodOptionDto> Methods { get; init; }
}

public sealed class CompletePaymentResponse
{
    public required int OrderId { get; init; }
    public required string OrderStatus { get; init; }
    public required string PaymentStatus { get; init; }
    public required bool IsPaid { get; init; }
    public string? Message { get; init; }
}

public sealed class TamaraNotificationPayload
{
    [JsonPropertyName("order_id")]
    public string? OrderId { get; set; }

    [JsonPropertyName("order_reference_id")]
    public string? OrderReferenceId { get; set; }

    [JsonPropertyName("order_status")]
    public string? OrderStatus { get; set; }

    [JsonPropertyName("payment_status")]
    public string? PaymentStatus { get; set; }
}

public sealed class TamaraWebhookAckResponse
{
    public required bool Success { get; init; }
    public string? Message { get; init; }
    public string? TamaraOrderId { get; init; }
}
