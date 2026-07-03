using IdealWeightNutrition.Contracts.Content;

namespace IdealWeightNutrition.Application.Abstractions;

public interface IVideoBannerService
{
    Task<VideoBannerDto> GetStatusAsync(CancellationToken cancellationToken = default);

    Task<VideoBannerDto> UploadVideoAsync(Stream stream, string fileName, CancellationToken cancellationToken = default);

    Task<VideoBannerDto> UploadPosterAsync(Stream stream, string fileName, CancellationToken cancellationToken = default);

    Task<VideoBannerDto> DeleteVideoAsync(CancellationToken cancellationToken = default);

    Task<VideoBannerDto> DeletePosterAsync(CancellationToken cancellationToken = default);
}
