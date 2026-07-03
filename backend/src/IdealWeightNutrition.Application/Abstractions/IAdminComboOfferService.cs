using IdealWeightNutrition.Contracts.Admin;

namespace IdealWeightNutrition.Application.Abstractions;

public interface IAdminComboOfferService
{
    Task<IReadOnlyList<AdminComboOfferListItemDto>> ListAsync(
        bool includeDeleted,
        CancellationToken cancellationToken = default);

    Task<AdminComboOfferDetailDto?> GetAsync(int id, CancellationToken cancellationToken = default);

    Task<AdminComboOfferDetailDto> CreateAsync(
        UpsertAdminComboOfferRequest request,
        CancellationToken cancellationToken = default);

    Task<AdminComboOfferDetailDto> UpdateAsync(
        int id,
        UpsertAdminComboOfferRequest request,
        CancellationToken cancellationToken = default);

    Task ToggleActiveAsync(int id, CancellationToken cancellationToken = default);

    Task SoftDeleteAsync(int id, CancellationToken cancellationToken = default);

    Task<AdminComboOfferItemDto> AddItemAsync(
        int comboOfferId,
        AddAdminComboOfferItemRequest request,
        CancellationToken cancellationToken = default);

    Task RemoveItemAsync(int itemId, CancellationToken cancellationToken = default);

    Task<AdminImageUploadResultDto> UploadImageAsync(
        int id,
        Stream fileStream,
        string fileName,
        CancellationToken cancellationToken = default);
}
