namespace IdealWeightNutrition.Infrastructure.Options;

public sealed class LegacyStorageOptions
{
    public const string SectionName = "LegacyStorage";

    /// <summary>
    /// Absolute path to legacy MVC wwwroot on the server (Images, videos, etc.).
    /// Example on SmarterASP: D:\home\site\wwwroot\legacy-media
    /// </summary>
    public string WwwRootPath { get; set; } = string.Empty;
}
