namespace IdealWeightNutrition.Contracts.Services;

public class ServicePurchaseSummaryDto
{
    public int Id { get; init; }
    public int ServiceSubscriptionId { get; init; }
    public required string ServiceTitle { get; init; }
    public string? ServiceTitleAr { get; init; }
    public string? ServiceImageUrl { get; init; }
    public required string ServiceType { get; init; }
    public double TotalAmount { get; init; }
    public double AmountPaid { get; init; }
    public double DiscountAmount { get; init; }
    public required string PaymentStatus { get; init; }
    public required string Status { get; init; }
    public DateTime PurchaseDate { get; init; }
}

public sealed class ServicePurchaseDetailDto : ServicePurchaseSummaryDto
{
    public string? ServiceDescription { get; init; }
    public string? ServiceDescriptionAr { get; init; }
}
