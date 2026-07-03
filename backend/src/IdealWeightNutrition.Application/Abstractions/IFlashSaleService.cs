using IdealWeightNutrition.Contracts.Promotions;

namespace IdealWeightNutrition.Application.Abstractions;

public interface IFlashSaleService
{
    Task<IReadOnlyList<FlashSaleSummaryDto>> ListActiveAsync(CancellationToken cancellationToken = default);

    Task<FlashSaleDetailDto?> GetActiveAsync(int id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<FlashSaleProductPriceDto>> ListActiveProductPricesAsync(
        CancellationToken cancellationToken = default);
}
