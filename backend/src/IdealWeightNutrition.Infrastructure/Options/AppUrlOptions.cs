namespace IdealWeightNutrition.Infrastructure.Options;

public sealed class AppUrlOptions
{
    public const string SectionName = "App";

    public string FrontendBaseUrl { get; set; } = "http://localhost:4200";

    public string PublicApiBaseUrl { get; set; } = "https://localhost:7128";
}
