using IdealWeightNutrition.Contracts.Admin;

namespace IdealWeightNutrition.Application.Abstractions;

public interface IAdminBlogService
{
    Task<IReadOnlyList<AdminBlogPostListItemDto>> ListAsync(
        bool includeDeleted,
        CancellationToken cancellationToken = default);

    Task<AdminBlogPostDetailDto?> GetAsync(int id, CancellationToken cancellationToken = default);

    Task<AdminBlogPostDetailDto> CreateAsync(
        UpsertAdminBlogPostRequest request,
        string? createdBy,
        CancellationToken cancellationToken = default);

    Task<AdminBlogPostDetailDto> UpdateAsync(
        int id,
        UpsertAdminBlogPostRequest request,
        string? modifiedBy,
        CancellationToken cancellationToken = default);

    Task SoftDeleteAsync(int id, CancellationToken cancellationToken = default);

    Task RestoreAsync(int id, CancellationToken cancellationToken = default);

    Task<AdminImageUploadResultDto> UploadImageAsync(
        int id,
        Stream fileStream,
        string fileName,
        CancellationToken cancellationToken = default);
}
