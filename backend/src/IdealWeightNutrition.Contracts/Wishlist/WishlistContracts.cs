namespace IdealWeightNutrition.Contracts.Wishlist;

public sealed class WishlistItemDto
{
    public required int Id { get; init; }
    public required int ProductId { get; init; }
    public required string Title { get; init; }
    public required string Slug { get; init; }
    public required string ImageUrl { get; init; }
    public required double Price { get; init; }
    public required double ListPrice { get; init; }
    public required bool InStock { get; init; }
    public required string ProductType { get; init; }
}

public sealed class WishlistResponse
{
    public required IReadOnlyList<WishlistItemDto> Items { get; init; }
    public required int Count { get; init; }
}

public sealed class WishlistToggleRequest
{
    public int ProductId { get; init; }
}

public sealed class WishlistToggleResponse
{
    public required bool IsInWishlist { get; init; }
    public required int WishlistCount { get; init; }
    public required string Message { get; init; }
}

public sealed class WishlistProductIdsResponse
{
    public required IReadOnlyList<int> ProductIds { get; init; }
}
