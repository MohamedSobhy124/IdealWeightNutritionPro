namespace IdealWeightNutrition.Contracts.Cart;

public sealed class CartItemDto
{
    public required string LineId { get; init; }
    public required int ProductId { get; init; }
    public int? ProductVariantId { get; init; }
    public required string Title { get; init; }
    public required string Slug { get; init; }
    public required string ImageUrl { get; init; }
    public required int Quantity { get; init; }
    public required double UnitPrice { get; init; }
    public required double LineTotal { get; init; }
    public required bool InStock { get; init; }
    public int MaxQuantity { get; init; }
    public int? FlashSaleItemId { get; init; }
    public int? ComboOfferId { get; init; }
}

public sealed class CartResponse
{
    public required IReadOnlyList<CartItemDto> Items { get; init; }
    public required int ItemCount { get; init; }
    public required double Subtotal { get; init; }
    public double Discount { get; init; }
    public double Total { get; init; }
    public CartPromoDto? AppliedPromo { get; init; }
}

public sealed class AddCartItemRequest
{
    public int ProductId { get; init; }
    public int Quantity { get; init; } = 1;
    public int? ProductVariantId { get; init; }
    public int? FlashSaleItemId { get; init; }
    public int? ComboOfferId { get; init; }
}

public sealed class UpdateCartItemRequest
{
    public int Quantity { get; init; }
}
