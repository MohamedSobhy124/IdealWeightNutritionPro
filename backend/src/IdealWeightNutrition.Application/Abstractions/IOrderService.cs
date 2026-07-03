using IdealWeightNutrition.Contracts.Orders;

namespace IdealWeightNutrition.Application.Abstractions;

public interface IOrderService
{
    Task<IReadOnlyList<OrderSummaryDto>> ListUserOrdersAsync(string userId, CancellationToken cancellationToken = default);
    Task<OrderDto?> GetOrderAsync(int orderId, string? userId, string? guestEmail, CancellationToken cancellationToken = default);
    Task<OrderDto?> TrackOrderAsync(TrackOrderRequest request, string? userId, CancellationToken cancellationToken = default);
}
