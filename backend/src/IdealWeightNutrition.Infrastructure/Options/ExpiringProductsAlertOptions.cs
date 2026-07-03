namespace IdealWeightNutrition.Infrastructure.Options;

public sealed class ExpiringProductsAlertOptions
{
    public const string SectionName = "ExpiringProductsAlert";

    public bool Enabled { get; set; } = true;

    public string AdminEmail { get; set; } = string.Empty;

    public int DaysBeforeExpiry { get; set; } = 10;

    public int CheckTimeHour { get; set; } = 8;
}
