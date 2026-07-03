using IdealWeightNutrition.Contracts.Admin;

namespace IdealWeightNutrition.Application.Abstractions;

public interface IAdminStockNotificationService
{
    Task<AdminStockNotificationListResponse> ListAsync(
        AdminStockNotificationQuery query,
        CancellationToken cancellationToken = default);

    Task<AdminStockNotificationActionResponse> DeactivateAsync(
        int id,
        CancellationToken cancellationToken = default);
}
