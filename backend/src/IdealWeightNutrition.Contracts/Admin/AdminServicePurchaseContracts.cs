namespace IdealWeightNutrition.Contracts.Admin;

public class AdminServicePurchaseListItemDto
{
    public required int Id { get; init; }
    public required int ServiceSubscriptionId { get; init; }
    public required string ServiceTitle { get; init; }
    public required string CustomerName { get; init; }
    public string? Email { get; init; }
    public string? Phone { get; init; }
    public required double TotalAmount { get; init; }
    public required double AmountPaid { get; init; }
    public required double DiscountAmount { get; init; }
    public required double VatAmount { get; init; }
    public required string PaymentStatus { get; init; }
    public required string ServiceStatus { get; init; }
    public required DateTime PurchaseDate { get; init; }
}

public sealed class AdminServicePurchaseDetailDto : AdminServicePurchaseListItemDto
{
    public int? ServiceOfferId { get; init; }
    public string? PaymentIntentId { get; init; }
    public string? SessionId { get; init; }
    public string? OfferSummary { get; init; }
    public bool IsGuest { get; init; }
}

public sealed class AdminServicePurchaseListResponse
{
    public required IReadOnlyList<AdminServicePurchaseListItemDto> Items { get; init; }
    public required int TotalCount { get; init; }
    public required int Page { get; init; }
    public required int PageSize { get; init; }
}

public sealed class AdminServicePurchaseQuery
{
    public string? PaymentStatus { get; init; }
    public string? ServiceStatus { get; init; }
    public DateTime? DateFrom { get; init; }
    public DateTime? DateTo { get; init; }
    public string? Search { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 25;
}

public sealed class UpdateAdminServicePurchaseRequest
{
    public string? PaymentStatus { get; init; }
    public string? ServiceStatus { get; init; }
    public decimal? AmountPaid { get; init; }
}

public sealed class AdminServicePurchaseActionResponse
{
    public required int PurchaseId { get; init; }
    public required string PaymentStatus { get; init; }
    public required string ServiceStatus { get; init; }
    public required double AmountPaid { get; init; }
    public required string Message { get; init; }
}
