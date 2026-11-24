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
    /// <summary>Whether ordering this combo can qualify for free delivery.</summary>
    public bool AllowFreeDelivery { get; set; }
    /// <summary>Cart subtotal (before promo discount) needed for free delivery when allowed. 0 = always free.</summary>
    public double FreeDeliveryMinimumAmount { get; set; }
    public ICollection<ComboOfferItem> Items { get; set; } = new List<ComboOfferItem>();
    public ICollection<ComboOfferImage> Images { get; set; } = new List<ComboOfferImage>();
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

public sealed class ComboOfferImage
{
    public int Id { get; set; }
    public int ComboOfferId { get; set; }
    public string ImageUrl { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }
    public string? ImageInfo { get; set; }
    public ComboOffer? ComboOffer { get; set; }
}
