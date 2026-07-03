using IdealWeightNutrition.Contracts.Admin;

namespace IdealWeightNutrition.Application.Abstractions;

public interface IAdminCatalogueService
{
    Task<IReadOnlyList<AdminCategoryDto>> ListCategoriesAsync(
        bool includeDeleted,
        CancellationToken cancellationToken = default);

    Task<AdminCategoryDto> CreateCategoryAsync(
        UpsertAdminCategoryRequest request,
        CancellationToken cancellationToken = default);

    Task<AdminCategoryDto> UpdateCategoryAsync(
        int id,
        UpsertAdminCategoryRequest request,
        CancellationToken cancellationToken = default);

    Task DeleteCategoryAsync(int id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AdminBrandDto>> ListBrandsAsync(
        bool includeDeleted,
        CancellationToken cancellationToken = default);

    Task<AdminBrandDto> CreateBrandAsync(
        UpsertAdminBrandRequest request,
        CancellationToken cancellationToken = default);

    Task<AdminBrandDto> UpdateBrandAsync(
        int id,
        UpsertAdminBrandRequest request,
        CancellationToken cancellationToken = default);

    Task DeleteBrandAsync(int id, CancellationToken cancellationToken = default);
}
