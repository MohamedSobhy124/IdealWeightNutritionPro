using IdealWeightNutrition.Contracts.Admin;

namespace IdealWeightNutrition.Application.Abstractions;

public interface IAdminServiceOfferService
{
    Task<IReadOnlyList<AdminServiceOfferListItemDto>> ListAsync(
        int? serviceSubscriptionId,
        CancellationToken cancellationToken = default);

    Task<AdminServiceOfferDetailDto?> GetAsync(int id, CancellationToken cancellationToken = default);

    Task<AdminServiceOfferDetailDto> CreateAsync(
        UpsertAdminServiceOfferRequest request,
        CancellationToken cancellationToken = default);

    Task<AdminServiceOfferDetailDto> UpdateAsync(
        int id,
        UpsertAdminServiceOfferRequest request,
        CancellationToken cancellationToken = default);

    Task ToggleActiveAsync(int id, CancellationToken cancellationToken = default);

    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
}
