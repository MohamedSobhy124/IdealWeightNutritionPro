using IdealWeightNutrition.Contracts.Admin;

namespace IdealWeightNutrition.Application.Abstractions;

public interface IAdminCompanyService
{
    Task<IReadOnlyList<AdminCompanyDto>> ListAsync(CancellationToken cancellationToken = default);
    Task<AdminCompanyDto?> GetAsync(int id, CancellationToken cancellationToken = default);
    Task<AdminCompanyDto> CreateAsync(UpsertAdminCompanyRequest request, CancellationToken cancellationToken = default);
    Task<AdminCompanyDto> UpdateAsync(int id, UpsertAdminCompanyRequest request, CancellationToken cancellationToken = default);
    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
}

public interface IAdminUserService
{
    Task<IReadOnlyList<AdminUserListItemDto>> ListAsync(CancellationToken cancellationToken = default);
    Task<CreateAdminUserResponse> CreateAsync(CreateAdminUserRequest request, CancellationToken cancellationToken = default);
}

public interface IAdminProductVariantService
{
    Task<IReadOnlyList<AdminProductOptionDto>> GetOptionsAsync(int productId, CancellationToken cancellationToken = default);
    Task<AdminProductOptionDto> AddOptionAsync(int productId, AddAdminProductOptionRequest request, CancellationToken cancellationToken = default);
    Task DeleteOptionAsync(int productId, int optionId, CancellationToken cancellationToken = default);
    Task<AdminProductOptionValueDto> AddOptionValueAsync(int productId, int optionId, AddAdminProductOptionValueRequest request, CancellationToken cancellationToken = default);
    Task DeleteOptionValueAsync(int productId, int valueId, CancellationToken cancellationToken = default);
    Task<GenerateVariantsResponse> GenerateVariantsAsync(int productId, CancellationToken cancellationToken = default);
    Task<AdminProductVariantDto> UpdateVariantAsync(int productId, int variantId, UpdateAdminProductVariantDetailRequest request, CancellationToken cancellationToken = default);
    Task<AdminImageUploadResultDto> UploadVariantImageAsync(
        int productId,
        int variantId,
        Stream fileStream,
        string fileName,
        CancellationToken cancellationToken = default);
    Task SetProductTypeAsync(int productId, string productType, CancellationToken cancellationToken = default);
}
