namespace IdealWeightNutrition.Infrastructure.Options;

public sealed class StockAlertOptions
{
    public const string SectionName = "StockAlerts";

    /// <summary>Primary admin inbox for return requests and stock alerts (legacy: StockAlerts:AdminEmail).</summary>
    public string? AdminEmail { get; set; }

    public bool DailyDigestEnabled { get; set; } = true;

    public int CheckTimeHour { get; set; } = 9;
}
