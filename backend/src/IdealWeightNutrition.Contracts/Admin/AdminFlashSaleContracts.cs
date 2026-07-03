namespace IdealWeightNutrition.Contracts.Admin;

public sealed class AdminFlashSaleListItemDto
{
    public required int Id { get; init; }
    public required string Name { get; init; }
    public required string NameAr { get; init; }
    public required string ImageUrl { get; init; }
    public required DateTime StartDate { get; init; }
    public required DateTime EndDate { get; init; }
    public required bool IsActive { get; init; }
    public required bool IsDeleted { get; init; }
    public required int ItemCount { get; init; }
}

public sealed class AdminFlashSaleItemDto
{
    public required int Id { get; init; }
    public required int ProductId { get; init; }
    public int? ProductVariantId { get; init; }
    public required string ProductTitle { get; init; }
    public required decimal FlashSalePrice { get; init; }
    public required int FlashSaleQuantity { get; init; }
    public required int FlashSaleQuantityCreated { get; init; }
    public required DateTime AddedDate { get; init; }
    public required bool IsDeleted { get; init; }
}

public sealed class AdminFlashSaleDetailDto
{
    public required int Id { get; init; }
    public required string Name { get; init; }
    public required string NameAr { get; init; }
    public required string Description { get; init; }
    public required string DescriptionAr { get; init; }
    public required string ImageUrl { get; init; }
    public required DateTime StartDate { get; init; }
    public required DateTime EndDate { get; init; }
    public required bool IsActive { get; init; }
    public required bool IsDeleted { get; init; }
    public required DateTime CreatedDate { get; init; }
    public required IReadOnlyList<AdminFlashSaleItemDto> Items { get; init; }
}

public sealed class UpsertAdminFlashSaleRequest
{
    public required string Name { get; init; }
    public required string NameAr { get; init; }
    public string Description { get; init; } = string.Empty;
    public string DescriptionAr { get; init; } = string.Empty;
    public string? ImageUrl { get; init; }
    public DateTime StartDate { get; init; }
    public DateTime EndDate { get; init; }
    public bool IsActive { get; init; } = true;
    public bool NotifySubscribers { get; init; }
}

public sealed class AddAdminFlashSaleItemRequest
{
    public int ProductId { get; init; }
    public int? ProductVariantId { get; init; }
    public decimal FlashSalePrice { get; init; }
    public int FlashSaleQuantity { get; init; }
}
