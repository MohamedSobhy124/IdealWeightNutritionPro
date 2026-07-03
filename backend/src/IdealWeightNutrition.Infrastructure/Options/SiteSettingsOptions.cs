namespace IdealWeightNutrition.Infrastructure.Options;

public sealed class SiteSettingsOptions
{
    public const string SectionName = "SiteSettings";

    public bool EnableReviewWithoutOrder { get; set; }
}
