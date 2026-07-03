namespace IdealWeightNutrition.Infrastructure.Options;

public sealed class CacheOptions
{
    public const string SectionName = "Cache";

    /// <summary>Minutes to cache catalogue reference data (categories, brands). 0 disables caching.</summary>
    public int ReferenceDataMinutes { get; set; } = 15;
}
