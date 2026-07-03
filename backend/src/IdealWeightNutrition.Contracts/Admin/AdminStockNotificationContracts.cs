namespace IdealWeightNutrition.Contracts.Admin;

public sealed class AdminStockNotificationQuery
{
    public bool ActiveOnly { get; init; } = true;
    public bool PendingOnly { get; init; }
    public string? Search { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 25;
}

public sealed class AdminStockNotificationListItemDto
{
    public required int Id { get; init; }
    public required int ProductId { get; init; }
    public required string ProductTitle { get; init; }
    public int? ProductVariantId { get; init; }
    public string? VariantSku { get; init; }
    public required string Email { get; init; }
    public string? PhoneNumber { get; init; }
    public required bool IsActive { get; init; }
    public required bool IsNotified { get; init; }
    public DateTime? NotifiedDate { get; init; }
    public required DateTime CreatedDate { get; init; }
}

public sealed class AdminStockNotificationListResponse
{
    public required IReadOnlyList<AdminStockNotificationListItemDto> Items { get; init; }
    public required int TotalCount { get; init; }
    public required int Page { get; init; }
    public required int PageSize { get; init; }
}

public sealed class AdminStockNotificationActionResponse
{
    public required int Id { get; init; }
    public required string Message { get; init; }
}
