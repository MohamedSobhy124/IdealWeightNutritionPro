namespace IdealWeightNutrition.Contracts.Content;

public sealed class VideoBannerDto
{
    public bool HasVideo { get; init; }
    public bool HasPoster { get; init; }
    public string? VideoUrl { get; init; }
    public string? PosterUrl { get; init; }
    public long? VideoSizeBytes { get; init; }
    public long? PosterSizeBytes { get; init; }
    public DateTime? VideoLastModified { get; init; }
    public DateTime? PosterLastModified { get; init; }
}
