namespace IdealWeightNutrition.Contracts.Admin;

public sealed class AdminComboOfferListItemDto
{
    public required int Id { get; init; }
    public required string Name { get; init; }
    public required string NameAr { get; init; }
    public required string ImageUrl { get; init; }
    public required decimal ComboPrice { get; init; }
    public required DateTime StartDate { get; init; }
    public required DateTime EndDate { get; init; }
    public required bool IsActive { get; init; }
    public required bool IsDeleted { get; init; }
    public required int ItemCount { get; init; }
    public required int DisplayOrder { get; init; }
}

public sealed class AdminComboOfferItemDto
{
    public required int Id { get; init; }
    public required int ProductId { get; init; }
    public int? ProductVariantId { get; init; }
    public required string ProductTitle { get; init; }
    public required int Quantity { get; init; }
    public required bool IsRequired { get; init; }
    public required int DisplayOrder { get; init; }
    public required bool IsDeleted { get; init; }
}

public sealed class AdminComboOfferDetailDto
{
    public required int Id { get; init; }
    public required string Name { get; init; }
    public required string NameAr { get; init; }
    public required string Description { get; init; }
    public required string DescriptionAr { get; init; }
    public required string ImageUrl { get; init; }
    public required decimal ComboPrice { get; init; }
    public required DateTime StartDate { get; init; }
    public required DateTime EndDate { get; init; }
    public required bool IsActive { get; init; }
    public required bool IsDeleted { get; init; }
    public required int MinimumQuantity { get; init; }
    public int? MaximumQuantity { get; init; }
    public required int DisplayOrder { get; init; }
    public required IReadOnlyList<AdminComboOfferItemDto> Items { get; init; }
}

public sealed class UpsertAdminComboOfferRequest
{
    public required string Name { get; init; }
    public required string NameAr { get; init; }
    public string Description { get; init; } = string.Empty;
    public string DescriptionAr { get; init; } = string.Empty;
    public string? ImageUrl { get; init; }
    public decimal ComboPrice { get; init; }
    public DateTime StartDate { get; init; }
    public DateTime EndDate { get; init; }
    public bool IsActive { get; init; } = true;
    public int MinimumQuantity { get; init; } = 1;
    public int? MaximumQuantity { get; init; }
    public int DisplayOrder { get; init; }
    public bool NotifySubscribers { get; init; }
}

public sealed class AddAdminComboOfferItemRequest
{
    public int ProductId { get; init; }
    public int? ProductVariantId { get; init; }
    public int Quantity { get; init; } = 1;
    public bool IsRequired { get; init; } = true;
}
