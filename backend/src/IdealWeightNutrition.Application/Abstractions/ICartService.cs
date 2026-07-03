using IdealWeightNutrition.Contracts.Cart;

namespace IdealWeightNutrition.Application.Abstractions;

public interface ICartService
{
    Task<CartResponse> GetCartAsync(string? userId, string? guestCartId, CancellationToken cancellationToken = default);
    Task<CartResponse> AddItemAsync(string? userId, string? guestCartId, AddCartItemRequest request, CancellationToken cancellationToken = default);
    Task<CartResponse> UpdateItemAsync(string? userId, string? guestCartId, string lineId, UpdateCartItemRequest request, CancellationToken cancellationToken = default);
    Task<CartResponse> RemoveItemAsync(string? userId, string? guestCartId, string lineId, CancellationToken cancellationToken = default);
    Task<CartResponse> ClearCartAsync(string? userId, string? guestCartId, CancellationToken cancellationToken = default);
    Task<CartResponse> ApplyPromoAsync(string? userId, string? guestCartId, ApplyPromoRequest request, CancellationToken cancellationToken = default);
    Task<CartResponse> RemovePromoAsync(string? userId, string? guestCartId, CancellationToken cancellationToken = default);
    Task<CartResponse> MergeGuestCartAsync(string userId, string? guestCartId, CancellationToken cancellationToken = default);
    string CreateGuestCartId();
}
