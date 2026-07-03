namespace IdealWeightNutrition.Contracts.Promotions;

public sealed class FlashSaleSummaryDto
{
    public required int Id { get; init; }
    public required string Name { get; init; }
    public string? NameAr { get; init; }
    public string? Description { get; init; }
    public required string ImageUrl { get; init; }
    public required DateTime StartDate { get; init; }
    public required DateTime EndDate { get; init; }
    public required int ProductCount { get; init; }
}

public sealed class FlashSaleItemDto
{
    public required int Id { get; init; }
    public required int ProductId { get; init; }
    public int? ProductVariantId { get; init; }
    public required string Title { get; init; }
    public required string Slug { get; init; }
    public required string ImageUrl { get; init; }
    public required double NormalPrice { get; init; }
    public required double FlashSalePrice { get; init; }
    public required int AvailableQuantity { get; init; }
    public required double DiscountPercent { get; init; }
    public required string ProductType { get; init; }
}

public sealed class FlashSaleDetailDto
{
    public required int Id { get; init; }
    public required string Name { get; init; }
    public string? NameAr { get; init; }
    public string? Description { get; init; }
    public required string ImageUrl { get; init; }
    public required DateTime StartDate { get; init; }
    public required DateTime EndDate { get; init; }
    public required IReadOnlyList<FlashSaleItemDto> Items { get; init; }
}

public sealed class FlashSaleProductPriceDto
{
    public required int ProductId { get; init; }
    public int? ProductVariantId { get; init; }
    public required double FlashSalePrice { get; init; }
}
