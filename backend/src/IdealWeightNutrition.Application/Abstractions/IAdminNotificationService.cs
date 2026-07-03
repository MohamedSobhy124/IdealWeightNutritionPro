namespace IdealWeightNutrition.Application.Abstractions;

public interface IAdminNotificationService
{
    Task NotifyNewReturnRequestAsync(int returnRequestId, CancellationToken cancellationToken = default);

    Task CheckProductStockLevelsAsync(int productId, CancellationToken cancellationToken = default);

    Task CheckVariantStockLevelsAsync(int variantId, CancellationToken cancellationToken = default);

    Task NotifyStockNotificationRequestAsync(
        int productId,
        string customerEmail,
        string? phoneNumber,
        int? variantId,
        CancellationToken cancellationToken = default);

    Task SendLowStockDigestAsync(CancellationToken cancellationToken = default);
}
