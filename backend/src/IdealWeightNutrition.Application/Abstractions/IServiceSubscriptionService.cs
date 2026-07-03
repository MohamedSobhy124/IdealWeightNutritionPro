using IdealWeightNutrition.Contracts.Services;

namespace IdealWeightNutrition.Application.Abstractions;

public interface IServiceSubscriptionService
{
    Task<IReadOnlyList<ServiceSubscriptionSummaryDto>> ListActiveAsync(CancellationToken cancellationToken = default);
    Task<ServiceSubscriptionDetailDto?> GetActiveAsync(int id, CancellationToken cancellationToken = default);
}
