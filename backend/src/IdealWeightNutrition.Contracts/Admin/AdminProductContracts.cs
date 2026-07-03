namespace IdealWeightNutrition.Contracts.Admin;

public sealed class AdminProductImageDto
{
    public required int Id { get; init; }
    public required string ImageUrl { get; init; }
    public required int DisplayOrder { get; init; }
    public string? ImageInfo { get; init; }
}

public sealed class UpdateProductInfoImageRequest
{
    public string? ImageInfo { get; init; }
}

public sealed class AdminProductListItemDto
{
    public required int Id { get; init; }
    public required string Title { get; init; }
    public required string Slug { get; init; }
    public required string ImageUrl { get; init; }
    public required double Price { get; init; }
    public required double ListPrice { get; init; }
    public required int StockQuantity { get; init; }
    public required string ProductType { get; init; }
    public string? CategoryName { get; init; }
    public string? BrandName { get; init; }
    public required bool IsDeleted { get; init; }
    public required bool InStock { get; init; }
    public required bool IsNew { get; init; }
    public required bool IsTrending { get; init; }
}

public sealed class AdminProductListResponse
{
    public required IReadOnlyList<AdminProductListItemDto> Items { get; init; }
    public required int TotalCount { get; init; }
    public required int Page { get; init; }
    public required int PageSize { get; init; }
}

public sealed class AdminProductVariantDto
{
    public required int Id { get; init; }
    public string? Sku { get; init; }
    public required string? VariantName { get; init; }
    public string? ImageUrl { get; init; }
    public required double Price { get; init; }
    public double? ListPrice { get; init; }
    public double? Price50 { get; init; }
    public double? Price100 { get; init; }
    public required int StockQuantity { get; init; }
    public required int MinimumStockAlert { get; init; }
    public DateTime? ExpiryDate { get; init; }
    public required bool IsDeleted { get; init; }
}

public sealed class AdminProductDetailDto
{
    public required int Id { get; init; }
    public required string Title { get; init; }
    public required string Slug { get; init; }
    public required string ImageUrl { get; init; }
    public required double Price { get; init; }
    public required double ListPrice { get; init; }
    public double? StoreCost { get; init; }
    public required int StockQuantity { get; init; }
    public required int MinimumStockAlert { get; init; }
    public required bool IsNew { get; init; }
    public required bool IsTrending { get; init; }
    public required bool AllowFreeDelivery { get; init; }
    public required double FreeDeliveryMinimumAmount { get; init; }
    public required string ProductType { get; init; }
    public string? CategoryName { get; init; }
    public string? BrandName { get; init; }
    public required bool IsDeleted { get; init; }
    public required IReadOnlyList<AdminProductVariantDto> Variants { get; init; }
    public required IReadOnlyList<AdminProductImageDto> Images { get; init; }
    public required IReadOnlyList<AdminProductOptionDto> Options { get; init; }
}

public sealed class UpdateAdminProductVariantRequest
{
    public int Id { get; init; }
    public double Price { get; init; }
    public int StockQuantity { get; init; }
    public double? ListPrice { get; init; }
}

public sealed class UpdateAdminProductRequest
{
    public required string Title { get; init; }
    public double Price { get; init; }
    public double ListPrice { get; init; }
    public double? StoreCost { get; init; }
    public int StockQuantity { get; init; }
    public int MinimumStockAlert { get; init; } = 5;
    public bool IsNew { get; init; }
    public bool IsTrending { get; init; }
    public bool AllowFreeDelivery { get; init; }
    public double FreeDeliveryMinimumAmount { get; init; }
    public bool IsDeleted { get; init; }
    public IReadOnlyList<UpdateAdminProductVariantRequest>? Variants { get; init; }
}

public sealed class CreateAdminProductRequest
{
    public required string Title { get; init; }
    public string? TitleAr { get; init; }
    public string? Description { get; init; }
    public string? DescriptionAr { get; init; }
    public string? Slug { get; init; }
    public required int CategoryId { get; init; }
    public int? BrandId { get; init; }
    public double Price { get; init; }
    public double ListPrice { get; init; }
    public double? StoreCost { get; init; }
    public int StockQuantity { get; init; }
    public int MinimumStockAlert { get; init; } = 5;
    public bool IsNew { get; init; }
    public bool IsTrending { get; init; }
    public bool AllowFreeDelivery { get; init; }
    public double FreeDeliveryMinimumAmount { get; init; }
    public DateTime? ExpiryDate { get; init; }
}

public sealed class RegenerateProductSlugsResponse
{
    public required bool Success { get; init; }
    public required string Message { get; init; }
    public int UpdatedCount { get; init; }
    public int TotalProducts { get; init; }
}
