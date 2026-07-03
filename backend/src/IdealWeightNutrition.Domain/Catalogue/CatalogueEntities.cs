namespace IdealWeightNutrition.Domain.Catalogue;

public enum ProductType
{
    Simple = 0,
    Variable = 1
}

public sealed class Product
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string TitleAr { get; set; } = string.Empty;
    public string? Author { get; set; }
    public string? ISBN { get; set; }
    public string? SlugEn { get; set; }
    public string Description { get; set; } = string.Empty;
    public string DescriptionAr { get; set; } = string.Empty;
    public string? SuggestedUse { get; set; }
    public string? SuggestedUseAr { get; set; }
    public string? HealthNotes { get; set; }
    public string? HealthNotesAr { get; set; }
    public string? Specification { get; set; }
    public string? SpecificationAr { get; set; }
    public double ListPrice { get; set; }
    public double Price { get; set; }
    public int CategryId { get; set; }
    public int? BrandId { get; set; }
    public string ImageUrl { get; set; } = string.Empty;
    public int StockQuantity { get; set; }
    public int MinimumStockAlert { get; set; } = 5;
    public ProductType ProductType { get; set; }
    public bool IsNew { get; set; }
    public bool IsTrending { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime CreatedDate { get; set; }
    public bool AllowFreeDelivery { get; set; }
    public double FreeDeliveryMinimumAmount { get; set; }
    public double? StoreCost { get; set; }

    public Category? Category { get; set; }
    public Brand? Brand { get; set; }
    public ICollection<ProductImage> Images { get; set; } = new List<ProductImage>();
    public ICollection<ProductVariant> Variants { get; set; } = new List<ProductVariant>();

    public string GetSlug() => !string.IsNullOrEmpty(SlugEn) ? SlugEn : Id.ToString();
    public bool IsInStock => ProductType == ProductType.Variable
        ? Variants.Any(v => !v.IsDeleted && v.StockQuantity > 0)
        : StockQuantity > 0;
}

public sealed class ProductImage
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public string ImageUrl { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }
    public string? ImageInfo { get; set; }
}

public sealed class ProductVariant
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public decimal Price { get; set; }
    public decimal? ListPrice { get; set; }
    public decimal? Price50 { get; set; }
    public decimal? Price100 { get; set; }
    public string? Sku { get; set; }
    public int StockQuantity { get; set; }
    public int MinimumStockAlert { get; set; } = 5;
    public string? ImageUrl { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public bool IsDeleted { get; set; }
    public ICollection<ProductVariantOptionValue> VariantOptionValues { get; set; } = new List<ProductVariantOptionValue>();
}

public sealed class ProductOption
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string NameAr { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }
    public bool IsDeleted { get; set; }
    public ICollection<ProductOptionValue> OptionValues { get; set; } = new List<ProductOptionValue>();
}

public sealed class ProductOptionValue
{
    public int Id { get; set; }
    public int ProductOptionId { get; set; }
    public string Value { get; set; } = string.Empty;
    public string ValueAr { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }
    public bool IsDeleted { get; set; }
    public ProductOption? ProductOption { get; set; }
}

public sealed class ProductVariantOptionValue
{
    public int Id { get; set; }
    public int ProductVariantId { get; set; }
    public int ProductOptionValueId { get; set; }
    public ProductOptionValue? OptionValue { get; set; }
}

public sealed class Category
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string NameAr { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string DescriptionAr { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public bool IsDeleted { get; set; }
}

public sealed class Brand
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string NameAr { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? DescriptionAr { get; set; }
    public string? ImageUrl { get; set; }
    public bool IsDeleted { get; set; }
}
