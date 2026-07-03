namespace IdealWeightNutrition.Contracts.Services;

public sealed class ServiceOfferDto
{
    public int Id { get; set; }
    public string DiscountType { get; set; } = string.Empty;
    public double DiscountValue { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
}

public class ServiceSubscriptionSummaryDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? TitleAr { get; set; }
    public string? Description { get; set; }
    public string? DescriptionAr { get; set; }
    public double Price { get; set; }
    public double? SalePrice { get; set; }
    public string ServiceType { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public bool HasActiveOffer { get; set; }
}

public sealed class ServiceSubscriptionDetailDto : ServiceSubscriptionSummaryDto
{
    public double? OfflinePaymentPercent { get; set; }
    public IReadOnlyList<string> ImageUrls { get; set; } = [];
    public IReadOnlyList<ServiceOfferDto> ActiveOffers { get; set; } = [];
}
