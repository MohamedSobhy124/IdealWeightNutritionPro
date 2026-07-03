using IdealWeightNutrition.Contracts.Admin;

namespace IdealWeightNutrition.Application.Abstractions;

public interface IAdminFlashSaleService
{
    Task<IReadOnlyList<AdminFlashSaleListItemDto>> ListAsync(
        bool includeDeleted,
        CancellationToken cancellationToken = default);

    Task<AdminFlashSaleDetailDto?> GetAsync(int id, CancellationToken cancellationToken = default);

    Task<AdminFlashSaleDetailDto> CreateAsync(
        UpsertAdminFlashSaleRequest request,
        CancellationToken cancellationToken = default);

    Task<AdminFlashSaleDetailDto> UpdateAsync(
        int id,
        UpsertAdminFlashSaleRequest request,
        CancellationToken cancellationToken = default);

    Task ToggleActiveAsync(int id, CancellationToken cancellationToken = default);

    Task SoftDeleteAsync(int id, CancellationToken cancellationToken = default);

    Task<AdminFlashSaleItemDto> AddItemAsync(
        int flashSaleId,
        AddAdminFlashSaleItemRequest request,
        CancellationToken cancellationToken = default);

    Task RemoveItemAsync(int itemId, CancellationToken cancellationToken = default);

    Task<AdminImageUploadResultDto> UploadImageAsync(
        int id,
        Stream fileStream,
        string fileName,
        CancellationToken cancellationToken = default);
}
