using IdealWeightNutrition.Contracts.Cart;

namespace IdealWeightNutrition.Application.Abstractions;

public interface IInventoryService
{
    Task EnsureStockAvailableAsync(IReadOnlyList<CartItemDto> items, CancellationToken cancellationToken = default);

    Task DeductStockForOrderAsync(int orderId, CancellationToken cancellationToken = default);

    Task RestoreStockForReturnAsync(int returnRequestId, CancellationToken cancellationToken = default);
}
