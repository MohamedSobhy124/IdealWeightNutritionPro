using Microsoft.AspNetCore.Hosting;

namespace IdealWeightNutrition.Infrastructure.Storage;

public enum LegacyMediaFolder
{
    FlashSales,
    ComboOffers,
    Blogs,
    Services
}

internal sealed class LegacyImageStorage
{
    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg", ".png", ".webp", ".gif"
    };

    private const long MaxUploadBytes = 5 * 1024 * 1024;

    private static readonly Dictionary<LegacyMediaFolder, (string SubFolder, string UrlPrefix)> Folders = new()
    {
        [LegacyMediaFolder.FlashSales] = ("images\\flashsales", @"\images\flashsales\"),
        [LegacyMediaFolder.ComboOffers] = ("images\\combooffers", @"\images\combooffers\"),
        [LegacyMediaFolder.Blogs] = ("images\\blogs", "/images/blogs/"),
        [LegacyMediaFolder.Services] = ("images\\services", @"\images\services\"),
    };

    private readonly IWebHostEnvironment _env;

    public LegacyImageStorage(IWebHostEnvironment env) => _env = env;

    public async Task<string> SaveAsync(
        LegacyMediaFolder folder,
        Stream fileStream,
        string fileName,
        CancellationToken cancellationToken = default)
    {
        var extension = Path.GetExtension(fileName);
        if (string.IsNullOrEmpty(extension) || !AllowedExtensions.Contains(extension))
            throw new InvalidOperationException("Unsupported image type. Use JPG, PNG, WEBP, or GIF.");

        var directory = GetDirectory(folder);
        Directory.CreateDirectory(directory);

        var storedName = $"{Guid.NewGuid():N}{extension.ToLowerInvariant()}";
        var physicalPath = Path.Combine(directory, storedName);

        await using (var output = File.Create(physicalPath))
        {
            await fileStream.CopyToAsync(output, cancellationToken);
        }

        if (new FileInfo(physicalPath).Length > MaxUploadBytes)
        {
            File.Delete(physicalPath);
            throw new InvalidOperationException("Image must be 5 MB or smaller.");
        }

        var (_, urlPrefix) = Folders[folder];
        return urlPrefix + storedName;
    }

    public void DeleteIfExists(string? imageUrl)
    {
        if (string.IsNullOrWhiteSpace(imageUrl))
            return;

        var relative = imageUrl.TrimStart('\\', '/').Replace('/', Path.DirectorySeparatorChar);
        var physicalPath = Path.Combine(GetLegacyWwwRoot(), relative);
        if (File.Exists(physicalPath))
            File.Delete(physicalPath);
    }

    public string GetDirectory(LegacyMediaFolder folder)
    {
        var (subFolder, _) = Folders[folder];
        return Path.Combine(GetLegacyWwwRoot(), subFolder);
    }

    private string GetLegacyWwwRoot() =>
        Path.GetFullPath(Path.Combine(
            _env.ContentRootPath,
            "..", "..", "..", "..",
            "IdealWeightNutrition",
            "wwwroot"));
}
