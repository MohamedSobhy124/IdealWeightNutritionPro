using IdealWeightNutrition.Contracts.Promotions;

namespace IdealWeightNutrition.Application.Abstractions;

public interface IComboOfferService
{
    Task<IReadOnlyList<ComboOfferSummaryDto>> ListActiveAsync(CancellationToken cancellationToken = default);

    Task<ComboOfferDetailDto?> GetActiveAsync(int id, CancellationToken cancellationToken = default);
}
