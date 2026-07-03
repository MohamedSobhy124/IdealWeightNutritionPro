namespace IdealWeightNutrition.Application.Abstractions;

public interface IOrderNotificationService
{
    Task SendOrderConfirmationAsync(int orderId, CancellationToken cancellationToken = default);

    Task SendOrderDeliveredAsync(int orderId, CancellationToken cancellationToken = default);

    Task<byte[]?> GenerateInvoicePdfAsync(
        int orderId,
        string? userId,
        string? guestEmail,
        CancellationToken cancellationToken = default);
}
