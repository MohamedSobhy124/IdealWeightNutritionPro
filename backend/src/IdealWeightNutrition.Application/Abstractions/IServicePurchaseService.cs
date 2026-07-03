using IdealWeightNutrition.Contracts.Services;

namespace IdealWeightNutrition.Application.Abstractions;

public interface IServicePurchaseService
{
    Task<IReadOnlyList<ServicePurchaseSummaryDto>> ListForUserAsync(
        string userId,
        CancellationToken cancellationToken = default);

    Task<ServicePurchaseDetailDto?> GetForUserAsync(
        int purchaseId,
        string userId,
        CancellationToken cancellationToken = default);
}
