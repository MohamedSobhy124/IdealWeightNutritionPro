namespace IdealWeightNutrition.Domain.Engagement;

public sealed class Review
{
    public int Id { get; set; }
    public int? ProductId { get; set; }
    public int? ServiceSubscriptionId { get; set; }
    public string UserId { get; set; } = string.Empty;
    public int Rating { get; set; }
    public string Comment { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public bool IsApproved { get; set; }
    public bool IsVerifiedPurchase { get; set; }
    public int HelpfulCount { get; set; }
}

public sealed class WishlistItem
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public string ApplicationUserId { get; set; } = string.Empty;
}

public sealed class NewsletterSubscription
{
    public int Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public DateTime SubscribedDate { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime? UnsubscribedDate { get; set; }
    public string? Source { get; set; }
}

public sealed class StockNotification
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public int? ProductVariantId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public string? ApplicationUserId { get; set; }
    public bool IsNotified { get; set; }
    public DateTime? NotifiedDate { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedDate { get; set; }
    public DateTime? ModifiedDate { get; set; }
    public bool IsDeleted { get; set; }
}
