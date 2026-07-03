using IdealWeightNutrition.Application.Abstractions;
using IdealWeightNutrition.Contracts.Content;
using IdealWeightNutrition.Infrastructure.Storage;

namespace IdealWeightNutrition.Infrastructure.Services;

internal sealed class VideoBannerService : IVideoBannerService
{
    private readonly VideoBannerStorage _storage;

    public VideoBannerService(VideoBannerStorage storage) => _storage = storage;

    public Task<VideoBannerDto> GetStatusAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(_storage.GetStatus());

    public async Task<VideoBannerDto> UploadVideoAsync(
        Stream stream,
        string fileName,
        CancellationToken cancellationToken = default)
    {
        await _storage.SaveVideoAsync(stream, fileName, cancellationToken);
        return _storage.GetStatus();
    }

    public async Task<VideoBannerDto> UploadPosterAsync(
        Stream stream,
        string fileName,
        CancellationToken cancellationToken = default)
    {
        await _storage.SavePosterAsync(stream, fileName, cancellationToken);
        return _storage.GetStatus();
    }

    public Task<VideoBannerDto> DeleteVideoAsync(CancellationToken cancellationToken = default)
    {
        _storage.DeleteVideo();
        return Task.FromResult(_storage.GetStatus());
    }

    public Task<VideoBannerDto> DeletePosterAsync(CancellationToken cancellationToken = default)
    {
        _storage.DeletePoster();
        return Task.FromResult(_storage.GetStatus());
    }
}
