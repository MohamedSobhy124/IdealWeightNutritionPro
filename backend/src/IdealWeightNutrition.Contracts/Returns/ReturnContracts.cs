namespace IdealWeightNutrition.Contracts.Returns;

public sealed class CreateReturnItemRequest
{
    public int OrderDetailId { get; init; }
    public int Quantity { get; init; }
    public string? ItemReason { get; init; }
    public string? ItemCondition { get; init; }
}

public sealed class CreateReturnRequest
{
    public int OrderId { get; init; }
    public string? Email { get; init; }
    public required string Reason { get; init; }
    public string? AdditionalNotes { get; init; }
    public required IReadOnlyList<CreateReturnItemRequest> Items { get; init; }
}

public sealed class ReturnItemDto
{
    public required int Id { get; init; }
    public required int OrderDetailId { get; init; }
    public required int ProductId { get; init; }
    public required string ProductTitle { get; init; }
    public required int Quantity { get; init; }
    public required decimal ReturnPrice { get; init; }
    public string? ItemReason { get; init; }
    public string? ItemCondition { get; init; }
}

public sealed class ReturnRequestDto
{
    public required int Id { get; init; }
    public required int OrderId { get; init; }
    public required string Status { get; init; }
    public required DateTime RequestDate { get; init; }
    public required string Reason { get; init; }
    public string? AdditionalNotes { get; init; }
    public string? RejectionReason { get; init; }
    public decimal? RefundAmount { get; init; }
    public string? RefundStatus { get; init; }
    public required IReadOnlyList<ReturnItemDto> Items { get; init; }
}

public sealed class ReturnListItemDto
{
    public required int Id { get; init; }
    public required int OrderId { get; init; }
    public required string Status { get; init; }
    public required DateTime RequestDate { get; init; }
    public string? CustomerEmail { get; init; }
    public decimal? RefundAmount { get; init; }
}

public sealed class ApproveReturnRequest
{
    public string? AdminNotes { get; init; }
    public string? ReturnTrackingNumber { get; init; }
    public string? ReturnCarrier { get; init; }
}

public sealed class RejectReturnRequest
{
    public required string RejectionReason { get; init; }
    public string? AdminNotes { get; init; }
}

public sealed class CompleteReturnRequest
{
    public string? RefundTransactionId { get; init; }
}

public sealed class CancelReturnRequest
{
    public string? Reason { get; init; }
    public string? AdminNotes { get; init; }
}

public sealed class RefundOrderRequest
{
    public decimal RefundAmount { get; init; }
    public string? Reason { get; init; }
}

public sealed class RefundOrderResponse
{
    public required int OrderId { get; init; }
    public required string OrderStatus { get; init; }
    public required string PaymentStatus { get; init; }
    public required decimal RefundAmount { get; init; }
    public string? GatewayRefundId { get; init; }
    public required string Message { get; init; }
}

public sealed class ReturnActionResponse
{
    public required int ReturnId { get; init; }
    public required string Status { get; init; }
    public required string Message { get; init; }
}
