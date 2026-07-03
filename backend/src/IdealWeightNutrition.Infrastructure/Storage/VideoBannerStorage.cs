using IdealWeightNutrition.Application.Abstractions;
using IdealWeightNutrition.Contracts.Content;
using Microsoft.AspNetCore.Hosting;

namespace IdealWeightNutrition.Infrastructure.Storage;

internal sealed class VideoBannerStorage
{
    private static readonly HashSet<string> VideoExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".mp4", ".webm", ".mov"
    };

    private static readonly HashSet<string> PosterExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg", ".png", ".webp"
    };

    private const long MaxVideoBytes = 50L * 1024 * 1024;
    private const long MaxPosterBytes = 5L * 1024 * 1024;

    private readonly IWebHostEnvironment _env;

    public VideoBannerStorage(IWebHostEnvironment env) => _env = env;

    public string VideoPath => Path.Combine(GetVideosDirectory(), "home-banner.mp4");

    public string PosterPath => Path.Combine(GetImagesDirectory(), "video-banner-poster.jpg");

    public string VideoUrl => "/videos/home-banner.mp4";

    public string PosterUrl => "/images/video-banner-poster.jpg";

    public async Task SaveVideoAsync(Stream stream, string fileName, CancellationToken cancellationToken = default)
    {
        var extension = Path.GetExtension(fileName);
        if (string.IsNullOrEmpty(extension) || !VideoExtensions.Contains(extension))
            throw new InvalidOperationException("Unsupported video type. Use MP4, WEBM, or MOV.");

        Directory.CreateDirectory(GetVideosDirectory());
        if (File.Exists(VideoPath))
            File.Delete(VideoPath);

        await using var output = File.Create(VideoPath);
        await stream.CopyToAsync(output, cancellationToken);

        if (new FileInfo(VideoPath).Length > MaxVideoBytes)
        {
            File.Delete(VideoPath);
            throw new InvalidOperationException("Video must be 50 MB or smaller.");
        }
    }

    public async Task SavePosterAsync(Stream stream, string fileName, CancellationToken cancellationToken = default)
    {
        var extension = Path.GetExtension(fileName);
        if (string.IsNullOrEmpty(extension) || !PosterExtensions.Contains(extension))
            throw new InvalidOperationException("Unsupported image type. Use JPG, PNG, or WEBP.");

        Directory.CreateDirectory(GetImagesDirectory());
        if (File.Exists(PosterPath))
            File.Delete(PosterPath);

        await using var output = File.Create(PosterPath);
        await stream.CopyToAsync(output, cancellationToken);

        if (new FileInfo(PosterPath).Length > MaxPosterBytes)
        {
            File.Delete(PosterPath);
            throw new InvalidOperationException("Poster image must be 5 MB or smaller.");
        }
    }

    public void DeleteVideo()
    {
        if (File.Exists(VideoPath))
            File.Delete(VideoPath);
    }

    public void DeletePoster()
    {
        if (File.Exists(PosterPath))
            File.Delete(PosterPath);
    }

    public VideoBannerDto GetStatus()
    {
        var hasVideo = File.Exists(VideoPath);
        var hasPoster = File.Exists(PosterPath);
        long? videoSize = null;
        long? posterSize = null;
        DateTime? videoModified = null;
        DateTime? posterModified = null;

        if (hasVideo)
        {
            var info = new FileInfo(VideoPath);
            videoSize = info.Length;
            videoModified = info.LastWriteTimeUtc;
        }

        if (hasPoster)
        {
            var info = new FileInfo(PosterPath);
            posterSize = info.Length;
            posterModified = info.LastWriteTimeUtc;
        }

        var version = videoModified ?? posterModified;
        var cacheBust = version?.Ticks.ToString();

        return new VideoBannerDto
        {
            HasVideo = hasVideo,
            HasPoster = hasPoster,
            VideoUrl = hasVideo ? AppendVersion(VideoUrl, cacheBust) : null,
            PosterUrl = hasPoster ? AppendVersion(PosterUrl, cacheBust) : null,
            VideoSizeBytes = videoSize,
            PosterSizeBytes = posterSize,
            VideoLastModified = videoModified,
            PosterLastModified = posterModified
        };
    }

    private string GetWwwRoot() =>
        Path.GetFullPath(Path.Combine(
            _env.ContentRootPath,
            "..", "..", "..", "..",
            "IdealWeightNutrition",
            "wwwroot"));

    private string GetVideosDirectory() => Path.Combine(GetWwwRoot(), "videos");

    private string GetImagesDirectory() => Path.Combine(GetWwwRoot(), "images");

    private static string AppendVersion(string url, string? version) =>
        string.IsNullOrEmpty(version) ? url : $"{url}?v={version}";
}
