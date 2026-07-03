namespace IdealWeightNutrition.Contracts.Reviews;

public sealed class ProductReviewDto
{
    public required int Id { get; init; }
    public required string UserName { get; init; }
    public required int Rating { get; init; }
    public required string Comment { get; init; }
    public required DateTime CreatedAt { get; init; }
    public required bool IsVerifiedPurchase { get; init; }
    public required int HelpfulCount { get; init; }
}

public sealed class ProductReviewSummaryDto
{
    public required double AverageRating { get; init; }
    public required int ReviewCount { get; init; }
}

public sealed class SubmitProductReviewRequest
{
    public int Rating { get; init; }
    public required string Comment { get; init; }
}

public sealed class FeaturedReviewDto
{
    public required int Id { get; init; }
    public required string UserName { get; init; }
    public string? Location { get; init; }
    public required int Rating { get; init; }
    public required string Comment { get; init; }
    public required bool IsVerifiedPurchase { get; init; }
}

public sealed class AdminReviewListItemDto
{
    public required int Id { get; init; }
    public int? ProductId { get; init; }
    public string? ProductTitle { get; init; }
    public int? ServiceSubscriptionId { get; init; }
    public string? ServiceTitle { get; init; }
    public required string ReviewType { get; init; }
    public required string UserName { get; init; }
    public required int Rating { get; init; }
    public required string Comment { get; init; }
    public required DateTime CreatedAt { get; init; }
    public required bool IsApproved { get; init; }
    public required bool IsVerifiedPurchase { get; init; }
}
