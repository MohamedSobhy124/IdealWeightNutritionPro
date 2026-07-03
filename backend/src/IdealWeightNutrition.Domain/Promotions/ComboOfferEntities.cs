namespace IdealWeightNutrition.Domain.Promotions;

public sealed class ComboOffer
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string NameAr { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string DescriptionAr { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty;
    public decimal ComboPrice { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public bool IsActive { get; set; }
    public bool IsDeleted { get; set; }
    public int MinimumQuantity { get; set; } = 1;
    public int? MaximumQuantity { get; set; }
    public int DisplayOrder { get; set; }
    public ICollection<ComboOfferItem> Items { get; set; } = new List<ComboOfferItem>();
}

public sealed class ComboOfferItem
{
    public int Id { get; set; }
    public int ComboOfferId { get; set; }
    public int ProductId { get; set; }
    public int? ProductVariantId { get; set; }
    public int Quantity { get; set; } = 1;
    public int DisplayOrder { get; set; }
    public bool IsRequired { get; set; } = true;
    public bool IsDeleted { get; set; }
    public ComboOffer? ComboOffer { get; set; }
}
