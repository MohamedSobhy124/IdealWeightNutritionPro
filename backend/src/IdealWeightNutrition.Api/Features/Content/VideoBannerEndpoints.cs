using FastEndpoints;
using IdealWeightNutrition.Application.Abstractions;
using IdealWeightNutrition.Contracts.Content;
using AuthPolicies = IdealWeightNutrition.Domain.Constants.Policies;

namespace IdealWeightNutrition.Api.Features.Content;

public sealed class GetVideoBannerEndpoint : EndpointWithoutRequest<VideoBannerDto>
{
    private readonly IVideoBannerService _banners;

    public GetVideoBannerEndpoint(IVideoBannerService banners) => _banners = banners;

    public override void Configure()
    {
        Get("content/video-banner");
        AllowAnonymous();
    }

    public override async Task HandleAsync(CancellationToken ct) =>
        await Send.OkAsync(await _banners.GetStatusAsync(ct), ct);
}

public sealed class GetAdminVideoBannerEndpoint : EndpointWithoutRequest<VideoBannerDto>
{
    private readonly IVideoBannerService _banners;

    public GetAdminVideoBannerEndpoint(IVideoBannerService banners) => _banners = banners;

    public override void Configure()
    {
        Get("admin/video-banner");
        Policies(AuthPolicies.Admin);
    }

    public override async Task HandleAsync(CancellationToken ct) =>
        await Send.OkAsync(await _banners.GetStatusAsync(ct), ct);
}

public sealed class UploadAdminVideoBannerVideoEndpoint : EndpointWithoutRequest<VideoBannerDto>
{
    private readonly IVideoBannerService _banners;

    public UploadAdminVideoBannerVideoEndpoint(IVideoBannerService banners) => _banners = banners;

    public override void Configure()
    {
        Post("admin/video-banner/video");
        Policies(AuthPolicies.Admin);
        AllowFileUploads();
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var file = Files.FirstOrDefault();
        if (file is null || file.Length == 0)
        {
            ThrowError("Please select a video file.", StatusCodes.Status400BadRequest);
            return;
        }

        await using var stream = file.OpenReadStream();
        var result = await _banners.UploadVideoAsync(stream, file.FileName, ct);
        await Send.OkAsync(result, ct);
    }
}

public sealed class UploadAdminVideoBannerPosterEndpoint : EndpointWithoutRequest<VideoBannerDto>
{
    private readonly IVideoBannerService _banners;

    public UploadAdminVideoBannerPosterEndpoint(IVideoBannerService banners) => _banners = banners;

    public override void Configure()
    {
        Post("admin/video-banner/poster");
        Policies(AuthPolicies.Admin);
        AllowFileUploads();
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var file = Files.FirstOrDefault();
        if (file is null || file.Length == 0)
        {
            ThrowError("Please select a poster image.", StatusCodes.Status400BadRequest);
            return;
        }

        await using var stream = file.OpenReadStream();
        var result = await _banners.UploadPosterAsync(stream, file.FileName, ct);
        await Send.OkAsync(result, ct);
    }
}

public sealed class DeleteAdminVideoBannerVideoEndpoint : EndpointWithoutRequest<VideoBannerDto>
{
    private readonly IVideoBannerService _banners;

    public DeleteAdminVideoBannerVideoEndpoint(IVideoBannerService banners) => _banners = banners;

    public override void Configure()
    {
        Delete("admin/video-banner/video");
        Policies(AuthPolicies.Admin);
    }

    public override async Task HandleAsync(CancellationToken ct) =>
        await Send.OkAsync(await _banners.DeleteVideoAsync(ct), ct);
}

public sealed class DeleteAdminVideoBannerPosterEndpoint : EndpointWithoutRequest<VideoBannerDto>
{
    private readonly IVideoBannerService _banners;

    public DeleteAdminVideoBannerPosterEndpoint(IVideoBannerService banners) => _banners = banners;

    public override void Configure()
    {
        Delete("admin/video-banner/poster");
        Policies(AuthPolicies.Admin);
    }

    public override async Task HandleAsync(CancellationToken ct) =>
        await Send.OkAsync(await _banners.DeletePosterAsync(ct), ct);
}
