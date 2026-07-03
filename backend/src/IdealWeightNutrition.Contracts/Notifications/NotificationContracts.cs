namespace IdealWeightNutrition.Contracts.Notifications;

public sealed class NotificationDto
{
    public required int Id { get; init; }
    public required string Title { get; init; }
    public required string Message { get; init; }
    public required string Type { get; init; }
    public required string Icon { get; init; }
    public required string Link { get; init; }
    public required bool IsRead { get; init; }
    public required DateTime CreatedAt { get; init; }
    public int? OrderId { get; init; }
    public int? RelatedId { get; init; }
}

public sealed class NotificationCountDto
{
    public required int Count { get; init; }
}

public sealed class NotificationActionResponse
{
    public required bool Success { get; init; }
}
