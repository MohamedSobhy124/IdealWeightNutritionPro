using IdealWeightNutrition.Contracts.Admin;

namespace IdealWeightNutrition.Application.Abstractions;

public interface IAdminServiceSubscriptionService
{
    Task<IReadOnlyList<AdminServiceListItemDto>> ListAsync(
        bool includeInactive,
        CancellationToken cancellationToken = default);

    Task<AdminServiceDetailDto?> GetAsync(int id, CancellationToken cancellationToken = default);

    Task<AdminServiceDetailDto> CreateAsync(
        UpsertAdminServiceRequest request,
        CancellationToken cancellationToken = default);

    Task<AdminServiceDetailDto> UpdateAsync(
        int id,
        UpsertAdminServiceRequest request,
        CancellationToken cancellationToken = default);

    Task ToggleActiveAsync(int id, CancellationToken cancellationToken = default);

    Task DeleteAsync(int id, CancellationToken cancellationToken = default);

    Task<AdminServiceImageDto> UploadImageAsync(
        int serviceId,
        Stream fileStream,
        string fileName,
        CancellationToken cancellationToken = default);

    Task DeleteImageAsync(int serviceId, int imageId, CancellationToken cancellationToken = default);
}
