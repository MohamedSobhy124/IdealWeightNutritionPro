namespace IdealWeightNutrition.Infrastructure.Options;

public sealed class CallOptions
{
    public const string SectionName = "Call";

    public bool Enabled { get; set; } = true;

    public string PhoneNumber { get; set; } = "971507700559";
}
