namespace IdealWeightNutrition.Contracts.Promotions;

public sealed class ComboOfferSummaryDto
{
    public required int Id { get; init; }
    public required string Name { get; init; }
    public string? NameAr { get; init; }
    public string? Description { get; init; }
    public required string ImageUrl { get; init; }
    public required double ComboPrice { get; init; }
    public required double OriginalPrice { get; init; }
    public required double SavingsPercent { get; init; }
    public required DateTime StartDate { get; init; }
    public required DateTime EndDate { get; init; }
    public required int ProductCount { get; init; }
    public required bool InStock { get; init; }
}

public sealed class ComboOfferLineItemDto
{
    public required int ProductId { get; init; }
    public int? ProductVariantId { get; init; }
    public required string Title { get; init; }
    public required string Slug { get; init; }
    public required int Quantity { get; init; }
    public required bool IsRequired { get; init; }
    public required bool InStock { get; init; }
}

public sealed class ComboOfferDetailDto
{
    public required int Id { get; init; }
    public required string Name { get; init; }
    public string? NameAr { get; init; }
    public string? Description { get; init; }
    public required string ImageUrl { get; init; }
    public required double ComboPrice { get; init; }
    public required double OriginalPrice { get; init; }
    public required double SavingsPercent { get; init; }
    public required DateTime StartDate { get; init; }
    public required DateTime EndDate { get; init; }
    public required int MaxQuantity { get; init; }
    public required bool InStock { get; init; }
    public required IReadOnlyList<ComboOfferLineItemDto> Items { get; init; }
}
