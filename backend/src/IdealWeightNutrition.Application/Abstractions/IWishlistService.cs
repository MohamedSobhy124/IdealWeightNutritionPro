using IdealWeightNutrition.Contracts.Wishlist;

namespace IdealWeightNutrition.Application.Abstractions;

public interface IWishlistService
{
    Task<WishlistResponse> GetWishlistAsync(string userId, CancellationToken cancellationToken = default);

    Task<WishlistProductIdsResponse> GetProductIdsAsync(string userId, CancellationToken cancellationToken = default);

    Task<WishlistToggleResponse> ToggleAsync(string userId, int productId, CancellationToken cancellationToken = default);

    Task RemoveAsync(string userId, int wishlistItemId, CancellationToken cancellationToken = default);
}
