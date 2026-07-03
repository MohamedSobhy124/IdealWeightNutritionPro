namespace IdealWeightNutrition.Contracts.Admin;

public sealed class AdminServiceListItemDto
{
    public required int Id { get; init; }
    public required string Title { get; init; }
    public string? TitleAr { get; init; }
    public required double Price { get; init; }
    public required string ServiceType { get; init; }
    public string? ImageUrl { get; init; }
    public required bool IsActive { get; init; }
    public required int DisplayOrder { get; init; }
    public required int ImageCount { get; init; }
    public required int PurchaseCount { get; init; }
}

public sealed class AdminServiceImageDto
{
    public required int Id { get; init; }
    public required string ImageUrl { get; init; }
    public required int DisplayOrder { get; init; }
}

public sealed class AdminServiceDetailDto
{
    public required int Id { get; init; }
    public required string Title { get; init; }
    public string? TitleAr { get; init; }
    public string? Description { get; init; }
    public string? DescriptionAr { get; init; }
    public required double Price { get; init; }
    public required string ServiceType { get; init; }
    public double? OfflinePaymentPercent { get; init; }
    public string? ImageUrl { get; init; }
    public required bool IsActive { get; init; }
    public required int DisplayOrder { get; init; }
    public required DateTime CreatedDate { get; init; }
    public DateTime? UpdatedDate { get; init; }
    public required IReadOnlyList<AdminServiceImageDto> Images { get; init; }
}

public sealed class UpsertAdminServiceRequest
{
    public required string Title { get; init; }
    public string? TitleAr { get; init; }
    public string? Description { get; init; }
    public string? DescriptionAr { get; init; }
    public required double Price { get; init; }
    public required string ServiceType { get; init; }
    public double? OfflinePaymentPercent { get; init; }
    public required bool IsActive { get; init; }
    public int DisplayOrder { get; init; }
}
