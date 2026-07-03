namespace IdealWeightNutrition.Contracts.Stock;

public sealed class StockNotificationSubscribeRequest
{
    public string? Email { get; init; }
    public string? PhoneNumber { get; init; }
    public int? ProductVariantId { get; init; }
}

public sealed class StockNotificationSubscribeResponse
{
    public required string Message { get; init; }
}
