using IdealWeightNutrition.Infrastructure.Options;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Options;

namespace IdealWeightNutrition.Infrastructure.Storage;

internal sealed class ProductStoragePathResolver
{
    private readonly IWebHostEnvironment _env;
    private readonly ProductStorageOptions _options;

    public ProductStoragePathResolver(IWebHostEnvironment env, IOptions<ProductStorageOptions> options)
    {
        _env = env;
        _options = options.Value;
    }

    public string GetProductsDirectory()
    {
        if (!string.IsNullOrWhiteSpace(_options.ProductsPath))
            return Path.GetFullPath(_options.ProductsPath);

        var legacyRelative = Path.Combine(
            _env.ContentRootPath,
            "..", "..", "..", "..",
            "IdealWeightNutrition",
            "wwwroot",
            "Images",
            "Products");

        return Path.GetFullPath(legacyRelative);
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
