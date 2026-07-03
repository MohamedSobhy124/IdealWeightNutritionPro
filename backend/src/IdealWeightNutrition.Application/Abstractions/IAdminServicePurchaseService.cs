using IdealWeightNutrition.Contracts.Admin;

namespace IdealWeightNutrition.Application.Abstractions;

public interface IAdminServicePurchaseService
{
    Task<AdminServicePurchaseListResponse> ListAsync(
        AdminServicePurchaseQuery query,
        CancellationToken cancellationToken = default);

    Task<AdminServicePurchaseDetailDto?> GetAsync(int id, CancellationToken cancellationToken = default);

    Task<byte[]> ExportCsvAsync(
        AdminServicePurchaseQuery query,
        CancellationToken cancellationToken = default);

    Task<AdminServicePurchaseStatisticsDto> GetStatisticsAsync(CancellationToken cancellationToken = default);

    Task<AdminServicePurchaseActionResponse> UpdateAsync(
        int id,
        UpdateAdminServicePurchaseRequest request,
        CancellationToken cancellationToken = default);
}
