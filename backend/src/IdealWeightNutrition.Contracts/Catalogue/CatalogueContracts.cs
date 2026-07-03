namespace IdealWeightNutrition.Contracts.Catalogue;

public sealed class ProductListItemDto
{
    public required int Id { get; init; }
    public required string Slug { get; init; }
    public required string Title { get; init; }
    public required string TitleAr { get; init; }
    public required double Price { get; init; }
    public required double ListPrice { get; init; }
    public required bool InStock { get; init; }
    public required string ImageUrl { get; init; }
    public int? CategoryId { get; init; }
    public string? CategoryName { get; init; }
    public int? BrandId { get; init; }
    public string? BrandName { get; init; }
    public bool IsNew { get; init; }
    public bool IsTrending { get; init; }
    public int? DisplayVariantId { get; init; }
    public required string ProductType { get; init; }
}

public sealed class ProductDetailDto
{
    public required int Id { get; init; }
    public required string Slug { get; init; }
    public required string Title { get; init; }
    public required string TitleAr { get; init; }
    public required string Description { get; init; }
    public required string DescriptionAr { get; init; }
    public string? SuggestedUse { get; init; }
    public string? SuggestedUseAr { get; init; }
    public string? HealthNotes { get; init; }
    public string? HealthNotesAr { get; init; }
    public string? Specification { get; init; }
    public string? SpecificationAr { get; init; }
    public required double Price { get; init; }
    public required double ListPrice { get; init; }
    public required bool InStock { get; init; }
    public required int StockQuantity { get; init; }
    public required string ProductType { get; init; }
    public required IReadOnlyList<string> ImageUrls { get; init; }
    public CategoryDto? Category { get; init; }
    public BrandDto? Brand { get; init; }
    public bool IsNew { get; init; }
    public bool IsTrending { get; init; }
    public IReadOnlyList<StorefrontProductOptionDto> Options { get; init; } = [];
    public IReadOnlyList<StorefrontProductVariantDto> Variants { get; init; } = [];
}

public sealed class StorefrontProductOptionDto
{
    public required int Id { get; init; }
    public required string Name { get; init; }
    public required string NameAr { get; init; }
    public required IReadOnlyList<StorefrontProductOptionValueDto> Values { get; init; }
}

public sealed class StorefrontProductOptionValueDto
{
    public required int Id { get; init; }
    public required string Value { get; init; }
    public required string ValueAr { get; init; }
}

public sealed class StorefrontProductVariantDto
{
    public required int Id { get; init; }
    public string? Sku { get; init; }
    public required double Price { get; init; }
    public double? ListPrice { get; init; }
    public required int StockQuantity { get; init; }
    public string? ImageUrl { get; init; }
    public required IReadOnlyList<int> OptionValueIds { get; init; }
}

public sealed class CategoryDto
{
    public required int Id { get; init; }
    public required string Name { get; init; }
    public required string NameAr { get; init; }
    public string? ImageUrl { get; init; }
}

public sealed class BrandDto
{
    public required int Id { get; init; }
    public required string Name { get; init; }
    public required string NameAr { get; init; }
    public string? ImageUrl { get; init; }
}

public sealed class ProductQueryRequest
{
    public string? Search { get; init; }
    public int? CategoryId { get; init; }
    public int? BrandId { get; init; }
    public string? Availability { get; init; }
    public string? SortBy { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}

public sealed class CategoryProductSectionDto
{
    public required CategoryDto Category { get; init; }
    public required IReadOnlyList<ProductListItemDto> Products { get; init; }
}
