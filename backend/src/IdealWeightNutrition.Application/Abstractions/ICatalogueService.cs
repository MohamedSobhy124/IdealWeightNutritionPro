using IdealWeightNutrition.Contracts.Catalogue;
using IdealWeightNutrition.Contracts.Common;

namespace IdealWeightNutrition.Application.Abstractions;

public interface ICatalogueService
{
    Task<PagedResult<ProductListItemDto>> ListProductsAsync(
        ProductQueryRequest query,
        CancellationToken cancellationToken = default);

    Task<ProductDetailDto?> GetProductBySlugAsync(
        string slug,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CategoryDto>> ListCategoriesAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<BrandDto>> ListBrandsAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ProductListItemDto>> ListDiscountedProductsAsync(
        int limit = 20,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CategoryProductSectionDto>> GetCategoryProductSectionsAsync(
        int maxCategories = 6,
        int productsPerCategory = 4,
        CancellationToken cancellationToken = default);
}
