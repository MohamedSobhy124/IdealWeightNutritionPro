namespace IdealWeightNutrition.Contracts.Newsletter;

public sealed class NewsletterSubscribeRequest
{
    public string? Email { get; init; }
    public string? Source { get; init; }
}

public sealed class NewsletterSubscribeResponse
{
    public required string Message { get; init; }
    public bool IsReactivated { get; init; }
}

public sealed class NewsletterStatusResponse
{
    public required bool IsSubscribed { get; init; }
}

public sealed class NewsletterUnsubscribeResponse
{
    public required string Message { get; init; }
}

public sealed class NewsletterUnsubscribeRequest
{
    public string? Email { get; init; }
}

public sealed class NewsletterSubscriptionDto
{
    public required int Id { get; init; }
    public required string Email { get; init; }
    public required DateTime SubscribedDate { get; init; }
    public required bool IsActive { get; init; }
    public DateTime? UnsubscribedDate { get; init; }
    public string? Source { get; init; }
}
