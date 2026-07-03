namespace IdealWeightNutrition.Domain.Promotions;

public sealed class FlashSale
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string NameAr { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string DescriptionAr { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public bool IsActive { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime CreatedDate { get; set; }
    public ICollection<FlashSaleItem> Items { get; set; } = new List<FlashSaleItem>();
}

public sealed class FlashSaleItem
{
    public int Id { get; set; }
    public int FlashSaleId { get; set; }
    public int ProductId { get; set; }
    public int? ProductVariantId { get; set; }
    public int FlashSaleQuantity { get; set; }
    public int FlashSaleQuantityCreated { get; set; }
    public decimal FlashSalePrice { get; set; }
    public DateTime AddedDate { get; set; }
    public DateTime CreatedDate { get; set; }
    public bool IsDeleted { get; set; }
    public FlashSale? FlashSale { get; set; }
}
