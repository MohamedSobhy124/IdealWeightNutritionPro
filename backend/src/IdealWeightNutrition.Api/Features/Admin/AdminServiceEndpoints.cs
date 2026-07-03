using FastEndpoints;
using IdealWeightNutrition.Application.Abstractions;
using IdealWeightNutrition.Contracts.Admin;
using AuthPolicies = IdealWeightNutrition.Domain.Constants.Policies;

namespace IdealWeightNutrition.Api.Features.Admin;

public sealed class ListAdminServicesRequest
{
    public bool IncludeInactive { get; init; }
}

public sealed class ListAdminServicesEndpoint : Endpoint<ListAdminServicesRequest, IReadOnlyList<AdminServiceListItemDto>>
{
    private readonly IAdminServiceSubscriptionService _services;

    public ListAdminServicesEndpoint(IAdminServiceSubscriptionService services) => _services = services;

    public override void Configure()
    {
        Get("admin/services");
        Policies(AuthPolicies.Admin);
    }

    public override async Task HandleAsync(ListAdminServicesRequest req, CancellationToken ct) =>
        await Send.OkAsync(await _services.ListAsync(req.IncludeInactive, ct), ct);
}

public sealed class GetAdminServiceEndpoint : EndpointWithoutRequest<AdminServiceDetailDto>
{
    private readonly IAdminServiceSubscriptionService _services;

    public GetAdminServiceEndpoint(IAdminServiceSubscriptionService services) => _services = services;

    public override void Configure()
    {
        Get("admin/services/{serviceId}");
        Policies(AuthPolicies.Admin);
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        if (!int.TryParse(Route<string>("serviceId"), out var id) || id <= 0)
        {
            ThrowError("Invalid service id.", StatusCodes.Status400BadRequest);
            return;
        }

        var service = await _services.GetAsync(id, ct);
        if (service is null)
            ThrowError("Service not found.", StatusCodes.Status404NotFound);
        else
            await Send.OkAsync(service, ct);
    }
}

public sealed class CreateAdminServiceEndpoint : Endpoint<UpsertAdminServiceRequest, AdminServiceDetailDto>
{
    private readonly IAdminServiceSubscriptionService _services;

    public CreateAdminServiceEndpoint(IAdminServiceSubscriptionService services) => _services = services;

    public override void Configure()
    {
        Post("admin/services");
        Policies(AuthPolicies.Admin);
    }

    public override async Task HandleAsync(UpsertAdminServiceRequest req, CancellationToken ct)
    {
        try
        {
            var service = await _services.CreateAsync(req, ct);
            HttpContext.Response.StatusCode = StatusCodes.Status201Created;
            await Send.OkAsync(service, ct);
        }
        catch (InvalidOperationException ex)
        {
            ThrowError(ex.Message, StatusCodes.Status400BadRequest);
        }
    }
}

public sealed class UpdateAdminServiceEndpoint : Endpoint<UpsertAdminServiceRequest, AdminServiceDetailDto>
{
    private readonly IAdminServiceSubscriptionService _services;

    public UpdateAdminServiceEndpoint(IAdminServiceSubscriptionService services) => _services = services;

    public override void Configure()
    {
        Put("admin/services/{serviceId}");
        Policies(AuthPolicies.Admin);
    }

    public override async Task HandleAsync(UpsertAdminServiceRequest req, CancellationToken ct)
    {
        if (!int.TryParse(Route<string>("serviceId"), out var id) || id <= 0)
        {
            ThrowError("Invalid service id.", StatusCodes.Status400BadRequest);
            return;
        }

        try
        {
            await Send.OkAsync(await _services.UpdateAsync(id, req, ct), ct);
        }
        catch (InvalidOperationException ex)
        {
            ThrowError(ex.Message, StatusCodes.Status400BadRequest);
        }
    }
}

public sealed class ToggleAdminServiceEndpoint : EndpointWithoutRequest
{
    private readonly IAdminServiceSubscriptionService _services;

    public ToggleAdminServiceEndpoint(IAdminServiceSubscriptionService services) => _services = services;

    public override void Configure()
    {
        Post("admin/services/{serviceId}/toggle");
        Policies(AuthPolicies.Admin);
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        if (!int.TryParse(Route<string>("serviceId"), out var id) || id <= 0)
        {
            ThrowError("Invalid service id.", StatusCodes.Status400BadRequest);
            return;
        }

        try
        {
            await _services.ToggleActiveAsync(id, ct);
            await Send.NoContentAsync(ct);
        }
        catch (InvalidOperationException ex)
        {
            ThrowError(ex.Message, StatusCodes.Status404NotFound);
        }
    }
}

public sealed class DeleteAdminServiceEndpoint : EndpointWithoutRequest
{
    private readonly IAdminServiceSubscriptionService _services;

    public DeleteAdminServiceEndpoint(IAdminServiceSubscriptionService services) => _services = services;

    public override void Configure()
    {
        Delete("admin/services/{serviceId}");
        Policies(AuthPolicies.Admin);
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        if (!int.TryParse(Route<string>("serviceId"), out var id) || id <= 0)
        {
            ThrowError("Invalid service id.", StatusCodes.Status400BadRequest);
            return;
        }

        try
        {
            await _services.DeleteAsync(id, ct);
            await Send.NoContentAsync(ct);
        }
        catch (InvalidOperationException ex)
        {
            ThrowError(ex.Message, StatusCodes.Status400BadRequest);
        }
    }
}

public sealed class UploadAdminServiceImageEndpoint : EndpointWithoutRequest<AdminServiceImageDto>
{
    private readonly IAdminServiceSubscriptionService _services;

    public UploadAdminServiceImageEndpoint(IAdminServiceSubscriptionService services) => _services = services;

    public override void Configure()
    {
        Post("admin/services/{serviceId}/images");
        Policies(AuthPolicies.Admin);
        AllowFileUploads();
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        if (!int.TryParse(Route<string>("serviceId"), out var serviceId) || serviceId <= 0)
        {
            ThrowError("Invalid service id.", StatusCodes.Status400BadRequest);
            return;
        }

        var file = HttpContext.Request.Form.Files.FirstOrDefault();
        if (file is null || file.Length == 0)
        {
            ThrowError("Image file is required.", StatusCodes.Status400BadRequest);
            return;
        }

        try
        {
            await using var stream = file.OpenReadStream();
            var image = await _services.UploadImageAsync(serviceId, stream, file.FileName, ct);
            HttpContext.Response.StatusCode = StatusCodes.Status201Created;
            await Send.OkAsync(image, ct);
        }
        catch (InvalidOperationException ex)
        {
            ThrowError(ex.Message, StatusCodes.Status400BadRequest);
        }
    }
}

public sealed class DeleteAdminServiceImageEndpoint : EndpointWithoutRequest
{
    private readonly IAdminServiceSubscriptionService _services;

    public DeleteAdminServiceImageEndpoint(IAdminServiceSubscriptionService services) => _services = services;

    public override void Configure()
    {
        Delete("admin/services/{serviceId}/images/{imageId}");
        Policies(AuthPolicies.Admin);
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        if (!int.TryParse(Route<string>("serviceId"), out var serviceId) || serviceId <= 0)
        {
            ThrowError("Invalid service id.", StatusCodes.Status400BadRequest);
            return;
        }

        if (!int.TryParse(Route<string>("imageId"), out var imageId) || imageId <= 0)
        {
            ThrowError("Invalid image id.", StatusCodes.Status400BadRequest);
            return;
        }

        try
        {
            await _services.DeleteImageAsync(serviceId, imageId, ct);
            await Send.NoContentAsync(ct);
        }
        catch (InvalidOperationException ex)
        {
            ThrowError(ex.Message, StatusCodes.Status404NotFound);
        }
    }
}

public sealed class UploadAdminFlashSaleImageEndpoint : EndpointWithoutRequest<AdminImageUploadResultDto>
{
    private readonly IAdminFlashSaleService _flashSales;

    public UploadAdminFlashSaleImageEndpoint(IAdminFlashSaleService flashSales) => _flashSales = flashSales;

    public override void Configure()
    {
        Post("admin/flash-sales/{flashSaleId}/image");
        Policies(AuthPolicies.Admin);
        AllowFileUploads();
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        if (!int.TryParse(Route<string>("flashSaleId"), out var id) || id <= 0)
        {
            ThrowError("Invalid flash sale id.", StatusCodes.Status400BadRequest);
            return;
        }

        var file = HttpContext.Request.Form.Files.FirstOrDefault();
        if (file is null || file.Length == 0)
        {
            ThrowError("Image file is required.", StatusCodes.Status400BadRequest);
            return;
        }

        try
        {
            await using var stream = file.OpenReadStream();
            var result = await _flashSales.UploadImageAsync(id, stream, file.FileName, ct);
            HttpContext.Response.StatusCode = StatusCodes.Status201Created;
            await Send.OkAsync(result, ct);
        }
        catch (InvalidOperationException ex)
        {
            ThrowError(ex.Message, StatusCodes.Status400BadRequest);
        }
    }
}

public sealed class UploadAdminComboOfferImageEndpoint : EndpointWithoutRequest<AdminImageUploadResultDto>
{
    private readonly IAdminComboOfferService _combos;

    public UploadAdminComboOfferImageEndpoint(IAdminComboOfferService combos) => _combos = combos;

    public override void Configure()
    {
        Post("admin/combo-offers/{comboOfferId}/image");
        Policies(AuthPolicies.Admin);
        AllowFileUploads();
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        if (!int.TryParse(Route<string>("comboOfferId"), out var id) || id <= 0)
        {
            ThrowError("Invalid combo offer id.", StatusCodes.Status400BadRequest);
            return;
        }

        var file = HttpContext.Request.Form.Files.FirstOrDefault();
        if (file is null || file.Length == 0)
        {
            ThrowError("Image file is required.", StatusCodes.Status400BadRequest);
            return;
        }

        try
        {
            await using var stream = file.OpenReadStream();
            var result = await _combos.UploadImageAsync(id, stream, file.FileName, ct);
            HttpContext.Response.StatusCode = StatusCodes.Status201Created;
            await Send.OkAsync(result, ct);
        }
        catch (InvalidOperationException ex)
        {
            ThrowError(ex.Message, StatusCodes.Status400BadRequest);
        }
    }
}

public sealed class UploadAdminBlogPostImageEndpoint : EndpointWithoutRequest<AdminImageUploadResultDto>
{
    private readonly IAdminBlogService _blog;

    public UploadAdminBlogPostImageEndpoint(IAdminBlogService blog) => _blog = blog;

    public override void Configure()
    {
        Post("admin/blog-posts/{blogPostId}/image");
        Policies(AuthPolicies.Admin);
        AllowFileUploads();
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        if (!int.TryParse(Route<string>("blogPostId"), out var id) || id <= 0)
        {
            ThrowError("Invalid blog post id.", StatusCodes.Status400BadRequest);
            return;
        }

        var file = HttpContext.Request.Form.Files.FirstOrDefault();
        if (file is null || file.Length == 0)
        {
            ThrowError("Image file is required.", StatusCodes.Status400BadRequest);
            return;
        }

        try
        {
            await using var stream = file.OpenReadStream();
            var result = await _blog.UploadImageAsync(id, stream, file.FileName, ct);
            HttpContext.Response.StatusCode = StatusCodes.Status201Created;
            await Send.OkAsync(result, ct);
        }
        catch (InvalidOperationException ex)
        {
            ThrowError(ex.Message, StatusCodes.Status400BadRequest);
        }
    }
}
