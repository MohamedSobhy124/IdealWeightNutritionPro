namespace IdealWeightNutrition.Infrastructure.Options;

public sealed class StockNotificationFulfillmentOptions
{
    public const string SectionName = "StockNotificationFulfillment";

    public bool Enabled { get; set; } = true;

    public int CheckIntervalMinutes { get; set; } = 15;
}
