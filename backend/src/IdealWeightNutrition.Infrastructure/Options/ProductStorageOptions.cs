namespace IdealWeightNutrition.Infrastructure.Options;

public sealed class ProductStorageOptions
{
    public const string SectionName = "ProductStorage";

    /// <summary>
    /// Absolute path to legacy wwwroot/Images/Products. When empty, resolved relative to the API content root.
    /// </summary>
    public string ProductsPath { get; set; } = string.Empty;
}
