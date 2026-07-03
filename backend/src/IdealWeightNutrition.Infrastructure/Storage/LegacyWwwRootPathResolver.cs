using IdealWeightNutrition.Infrastructure.Options;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Options;

namespace IdealWeightNutrition.Infrastructure.Storage;

internal sealed class LegacyWwwRootPathResolver
{
    public LegacyWwwRootPathResolver(IWebHostEnvironment env, IOptions<LegacyStorageOptions> options)
    {
        if (!string.IsNullOrWhiteSpace(options.Value.WwwRootPath))
        {
            Path = System.IO.Path.GetFullPath(options.Value.WwwRootPath);
            return;
        }

        Path = System.IO.Path.GetFullPath(System.IO.Path.Combine(
            env.ContentRootPath,
            "..", "..", "..", "..",
            "IdealWeightNutrition",
            "wwwroot"));
    }

    public string Path { get; }

    public bool Exists => Directory.Exists(Path);
}
