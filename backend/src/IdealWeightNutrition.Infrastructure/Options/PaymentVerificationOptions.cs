namespace IdealWeightNutrition.Infrastructure.Options;

public sealed class PaymentVerificationOptions
{
    public const string SectionName = "PaymentVerification";

    public bool Enabled { get; set; } = true;

    public int CheckIntervalMinutes { get; set; } = 5;

    public int PendingOrderThresholdMinutes { get; set; } = 20;

    public int CancelAfterMinutes { get; set; } = 20;
}
