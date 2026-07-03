using IdealWeightNutrition.Contracts.Admin;

namespace IdealWeightNutrition.Application.Abstractions;

public interface IAdminDeliveryService
{
    Task<IReadOnlyList<AdminCityDto>> ListCitiesAsync(CancellationToken cancellationToken = default);

    Task<AdminCityDto> CreateCityAsync(
        UpsertAdminCityRequest request,
        CancellationToken cancellationToken = default);

    Task<AdminCityDto> UpdateCityAsync(
        int id,
        UpsertAdminCityRequest request,
        CancellationToken cancellationToken = default);

    Task DeleteCityAsync(int id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AdminRemoteAreaDto>> ListRemoteAreasAsync(
        int cityId,
        CancellationToken cancellationToken = default);

    Task<AdminRemoteAreaDto> CreateRemoteAreaAsync(
        int cityId,
        UpsertAdminRemoteAreaRequest request,
        CancellationToken cancellationToken = default);

    Task<AdminRemoteAreaDto> UpdateRemoteAreaAsync(
        int id,
        UpsertAdminRemoteAreaRequest request,
        CancellationToken cancellationToken = default);

    Task DeleteRemoteAreaAsync(int id, CancellationToken cancellationToken = default);
}
