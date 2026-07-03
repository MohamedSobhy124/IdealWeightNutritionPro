namespace IdealWeightNutrition.Contracts.Orders;

public sealed class OrderLineDto
{
    public int? OrderDetailId { get; init; }
    public required int ProductId { get; init; }
    public required string Title { get; init; }
    public required string Slug { get; init; }
    public required int Quantity { get; init; }
    public required double UnitPrice { get; init; }
    public required double LineTotal { get; init; }
}

public sealed class OrderSummaryDto
{
    public required int Id { get; init; }
    public required DateTime OrderDate { get; init; }
    public required string OrderStatus { get; init; }
    public required string PaymentStatus { get; init; }
    public required double OrderTotal { get; init; }
}

public sealed class OrderDto
{
    public required int Id { get; init; }
    public required DateTime OrderDate { get; init; }
    public required string OrderStatus { get; init; }
    public required string PaymentStatus { get; init; }
    public string? PaymentMethod { get; init; }
    public required double OrderTotal { get; init; }
    public double? OrderSubtotal { get; init; }
    public required double Shipping { get; init; }
    public required string Name { get; init; }
    public string? Email { get; init; }
    public required string PhoneNumber { get; init; }
    public required string StreetAddress { get; init; }
    public required string City { get; init; }
    public string? Area { get; init; }
    public required IReadOnlyList<OrderLineDto> Items { get; init; }
}

public sealed class TrackOrderRequest
{
    public int OrderId { get; init; }
    public required string Email { get; init; }
}

public sealed class AdminOrderListItemDto
{
    public required int Id { get; init; }
    public required DateTime OrderDate { get; init; }
    public required string CustomerName { get; init; }
    public string? Email { get; init; }
    public required string OrderStatus { get; init; }
    public required string PaymentStatus { get; init; }
    public required double OrderTotal { get; init; }
    public required string City { get; init; }
    public bool IsGuestOrder { get; init; }
}

public sealed class AdminOrderDetailDto
{
    public required int Id { get; init; }
    public required DateTime OrderDate { get; init; }
    public DateTime? ShippingDate { get; init; }
    public required string OrderStatus { get; init; }
    public required string PaymentStatus { get; init; }
    public string? PaymentMethod { get; init; }
    public required double OrderTotal { get; init; }
    public double? OrderSubtotal { get; init; }
    public double? DiscountAmount { get; init; }
    public string? PromoCodeText { get; init; }
    public required double Shipping { get; init; }
    public required string Name { get; init; }
    public string? Email { get; init; }
    public required string PhoneNumber { get; init; }
    public required string StreetAddress { get; init; }
    public required string City { get; init; }
    public string? Area { get; init; }
    public string? TrackingNumber { get; init; }
    public string? Carrier { get; init; }
    public bool IsGuestOrder { get; init; }
    public required IReadOnlyList<OrderLineDto> Items { get; init; }
}

public sealed class ShipOrderRequest
{
    public required string Carrier { get; init; }
    public required string TrackingNumber { get; init; }
}

public sealed class AdminOrderActionResponse
{
    public required int OrderId { get; init; }
    public required string OrderStatus { get; init; }
    public required string PaymentStatus { get; init; }
    public required string Message { get; init; }
}

public sealed class ForceOrderActionRequest
{
    public string? Reason { get; init; }
}

public sealed class UpdateOrderLineRequest
{
    public required int OrderDetailId { get; init; }
    public required int Quantity { get; init; }
    public required double UnitPrice { get; init; }
}

public sealed class AdminOrderQuery
{
    public string? Status { get; init; }
    public string? PaymentStatus { get; init; }
    public string? PaymentMethod { get; init; }
    public DateTime? DateFrom { get; init; }
    public DateTime? DateTo { get; init; }
    public string? Search { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 50;
}

public sealed class AdminOrderListResponse
{
    public required IReadOnlyList<AdminOrderListItemDto> Items { get; init; }
    public required int TotalCount { get; init; }
    public required int Page { get; init; }
    public required int PageSize { get; init; }
}

public sealed class AdminOrderStatisticsDto
{
    public required int All { get; init; }
    public required int Pending { get; init; }
    public required int Approved { get; init; }
    public required int Processing { get; init; }
    public required int Shipped { get; init; }
    public required int Delivered { get; init; }
    public required int Cancelled { get; init; }
}

public sealed class AdminOrderAuditLogDto
{
    public required int Id { get; init; }
    public required int OrderHeaderId { get; init; }
    public required string Action { get; init; }
    public string? ActionDetails { get; init; }
    public string? PerformedByUserId { get; init; }
    public string? PerformedByUserEmail { get; init; }
    public string? OldOrderStatus { get; init; }
    public string? NewOrderStatus { get; init; }
    public string? OldPaymentStatus { get; init; }
    public string? NewPaymentStatus { get; init; }
    public required DateTime ActionDate { get; init; }
    public string? IpAddress { get; init; }
    public string? UserAgent { get; init; }
}
