using IdealWeightNutrition.Contracts.Admin;

namespace IdealWeightNutrition.Application.Abstractions;

public interface IAdminPromoCodeService
{
    Task<IReadOnlyList<AdminPromoCodeListItemDto>> ListAsync(CancellationToken cancellationToken = default);

    Task<AdminPromoCodeDetailDto?> GetAsync(int id, CancellationToken cancellationToken = default);

    Task<AdminPromoCodeDetailDto> CreateAsync(
        UpsertAdminPromoCodeRequest request,
        string? createdBy,
        CancellationToken cancellationToken = default);

    Task<AdminPromoCodeDetailDto> UpdateAsync(
        int id,
        UpsertAdminPromoCodeRequest request,
        CancellationToken cancellationToken = default);

    Task ToggleActiveAsync(int id, CancellationToken cancellationToken = default);

    Task DeleteAsync(int id, CancellationToken cancellationToken = default);

    Task<PromoCodeExclusionsDto> GetExclusionsAsync(int promoId, CancellationToken cancellationToken = default);

    Task AddExcludedProductAsync(int promoId, int productId, CancellationToken cancellationToken = default);

    Task RemoveExcludedProductAsync(int exclusionId, CancellationToken cancellationToken = default);

    Task AddExcludedComboOfferAsync(int promoId, int comboOfferId, CancellationToken cancellationToken = default);

    Task RemoveExcludedComboOfferAsync(int exclusionId, CancellationToken cancellationToken = default);

    Task AddExcludedServiceAsync(int promoId, int serviceSubscriptionId, CancellationToken cancellationToken = default);

    Task RemoveExcludedServiceAsync(int exclusionId, CancellationToken cancellationToken = default);
}
