namespace IdealWeightNutrition.Application.Abstractions;

public interface IStockNotificationFulfillmentService
{
    Task<int> ProcessPendingAsync(CancellationToken cancellationToken = default);

    Task<int> ProcessProductRestockedAsync(
        int productId,
        int? productVariantId = null,
        CancellationToken cancellationToken = default);
}
