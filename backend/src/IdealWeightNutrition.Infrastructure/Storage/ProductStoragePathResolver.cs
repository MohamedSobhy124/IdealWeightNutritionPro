using IdealWeightNutrition.Infrastructure.Options;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Options;

namespace IdealWeightNutrition.Infrastructure.Storage;

internal sealed class ProductStoragePathResolver
{
    private readonly LegacyWwwRootPathResolver _wwwRoot;
    private readonly ProductStorageOptions _options;

    public ProductStoragePathResolver(LegacyWwwRootPathResolver wwwRoot, IOptions<ProductStorageOptions> options)
    {
        _wwwRoot = wwwRoot;
        _options = options.Value;
    }

    public string GetProductsDirectory()
    {
        if (!string.IsNullOrWhiteSpace(_options.ProductsPath))
            return Path.GetFullPath(_options.ProductsPath);

        return Path.Combine(_wwwRoot.Path, "Images", "Products");
    }

    public void EnsureProductsDirectoryExists()
    {
        var dir = GetProductsDirectory();
        Directory.CreateDirectory(dir);
    }

    public string GetProductVariantsDirectory()
    {
        return Path.Combine(GetProductsDirectory(), "Variants");
    }

    public void EnsureProductVariantsDirectoryExists()
    {
        Directory.CreateDirectory(GetProductVariantsDirectory());
    }
}
