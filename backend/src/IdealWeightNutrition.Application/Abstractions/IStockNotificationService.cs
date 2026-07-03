using IdealWeightNutrition.Contracts.Stock;

namespace IdealWeightNutrition.Application.Abstractions;

public interface IStockNotificationService
{
    Task<StockNotificationSubscribeResponse> SubscribeAsync(
        int productId,
        string? email,
        string? phoneNumber,
        int? productVariantId,
        string? userId,
        CancellationToken cancellationToken = default);
}
