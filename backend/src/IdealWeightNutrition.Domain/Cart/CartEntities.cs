namespace IdealWeightNutrition.Domain.Cart;

public sealed class ShoppingCartLine
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public int Count { get; set; }
    public string ApplicationUserId { get; set; } = string.Empty;
    public int? FlashSaleItemId { get; set; }
    public decimal? FlashSalePrice { get; set; }
    public int? ProductVariantId { get; set; }
    public int? ComboOfferId { get; set; }
}

public sealed class GuestCartLine
{
    public Guid LineId { get; set; }
    public int ProductId { get; set; }
    public int? ProductVariantId { get; set; }
    public int? FlashSaleItemId { get; set; }
    public decimal? FlashSalePrice { get; set; }
    public int? ComboOfferId { get; set; }
    public int Quantity { get; set; }
}

public sealed class GuestCart
{
    public List<GuestCartLine> Items { get; set; } = new();
}
