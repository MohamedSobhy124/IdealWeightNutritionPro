using FastEndpoints;
using IdealWeightNutrition.Application.Abstractions;
using IdealWeightNutrition.Contracts.Admin;
using AuthPolicies = IdealWeightNutrition.Domain.Constants.Policies;
using IDateTimeProvider = IdealWeightNutrition.Application.Abstractions.IDateTimeProvider;

namespace IdealWeightNutrition.Api.Features.Admin;

public sealed class ListAdminProductsRequest
{
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 50;
    public string? Search { get; init; }
    public string Filter { get; init; } = "active";
    public bool IncludeDeleted { get; init; }
}

public sealed class ListAdminProductsEndpoint : Endpoint<ListAdminProductsRequest, AdminProductListResponse>
{
    private readonly IAdminProductService _products;

    public ListAdminProductsEndpoint(IAdminProductService products) => _products = products;

    public override void Configure()
    {
        Get("admin/products");
        Policies(AuthPolicies.Admin);
    }

    public override async Task HandleAsync(ListAdminProductsRequest req, CancellationToken ct)
    {
        var result = await _products.ListProductsAsync(
            req.Page,
            req.PageSize,
            req.Search,
            req.Filter,
            req.IncludeDeleted,
            ct);
        await Send.OkAsync(result, ct);
    }
}

public sealed class GetAdminProductEndpoint : EndpointWithoutRequest<AdminProductDetailDto>
{
    private readonly IAdminProductService _products;

    public GetAdminProductEndpoint(IAdminProductService products) => _products = products;

    public override void Configure()
    {
        Get("admin/products/{productId}");
        Policies(AuthPolicies.Admin);
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        if (!int.TryParse(Route<string>("productId"), out var productId) || productId <= 0)
        {
            ThrowError("Invalid product id.", StatusCodes.Status400BadRequest);
            return;
        }

        var product = await _products.GetProductAsync(productId, ct);
        if (product is null)
            ThrowError("Product not found.", StatusCodes.Status404NotFound);
        else
            await Send.OkAsync(product, ct);
    }
}

public sealed class UpdateAdminProductEndpoint : Endpoint<UpdateAdminProductRequest, AdminProductDetailDto>
{
    private readonly IAdminProductService _products;

    public UpdateAdminProductEndpoint(IAdminProductService products) => _products = products;

    public override void Configure()
    {
        Put("admin/products/{productId}");
        Policies(AuthPolicies.Admin);
    }

    public override async Task HandleAsync(UpdateAdminProductRequest req, CancellationToken ct)
    {
        if (!int.TryParse(Route<string>("productId"), out var productId) || productId <= 0)
        {
            ThrowError("Invalid product id.", StatusCodes.Status400BadRequest);
            return;
        }

        try
        {
            var product = await _products.UpdateProductAsync(productId, req, ct);
            await Send.OkAsync(product, ct);
        }
        catch (InvalidOperationException ex)
        {
            ThrowError(ex.Message, StatusCodes.Status400BadRequest);
        }
    }
}

public sealed class CreateAdminProductEndpoint : Endpoint<CreateAdminProductRequest, AdminProductDetailDto>
{
    private readonly IAdminProductService _products;

    public CreateAdminProductEndpoint(IAdminProductService products) => _products = products;

    public override void Configure()
    {
        Post("admin/products");
        Policies(AuthPolicies.Admin);
    }

    public override async Task HandleAsync(CreateAdminProductRequest req, CancellationToken ct)
    {
        try
        {
            var product = await _products.CreateProductAsync(req, ct);
            HttpContext.Response.StatusCode = StatusCodes.Status201Created;
            await Send.OkAsync(product, ct);
        }
        catch (InvalidOperationException ex)
        {
            ThrowError(ex.Message, StatusCodes.Status400BadRequest);
        }
    }
}

public sealed class UploadAdminProductImageEndpoint : EndpointWithoutRequest<AdminProductImageDto>
{
    private readonly IAdminProductService _products;

    public UploadAdminProductImageEndpoint(IAdminProductService products) => _products = products;

    public override void Configure()
    {
        Post("admin/products/{productId}/images");
        Policies(AuthPolicies.Admin);
        AllowFileUploads();
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        if (!int.TryParse(Route<string>("productId"), out var productId) || productId <= 0)
        {
            ThrowError("Invalid product id.", StatusCodes.Status400BadRequest);
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
            var image = await _products.UploadProductImageAsync(productId, stream, file.FileName, ct);
            HttpContext.Response.StatusCode = StatusCodes.Status201Created;
            await Send.OkAsync(image, ct);
        }
        catch (InvalidOperationException ex)
        {
            ThrowError(ex.Message, StatusCodes.Status400BadRequest);
        }
    }
}

public sealed class DeleteAdminProductImageEndpoint : EndpointWithoutRequest
{
    private readonly IAdminProductService _products;

    public DeleteAdminProductImageEndpoint(IAdminProductService products) => _products = products;

    public override void Configure()
    {
        Delete("admin/products/{productId}/images/{imageId}");
        Policies(AuthPolicies.Admin);
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        if (!int.TryParse(Route<string>("productId"), out var productId) || productId <= 0
            || !int.TryParse(Route<string>("imageId"), out var imageId) || imageId <= 0)
        {
            ThrowError("Invalid product or image id.", StatusCodes.Status400BadRequest);
            return;
        }

        try
        {
            await _products.DeleteProductImageAsync(productId, imageId, ct);
            await Send.NoContentAsync(ct);
        }
        catch (InvalidOperationException ex)
        {
            ThrowError(ex.Message, StatusCodes.Status400BadRequest);
        }
    }
}

public sealed class UploadAdminProductInfoImageEndpoint : EndpointWithoutRequest<AdminProductImageDto>
{
    private readonly IAdminProductService _products;

    public UploadAdminProductInfoImageEndpoint(IAdminProductService products) => _products = products;

    public override void Configure()
    {
        Post("admin/products/{productId}/info-images");
        Policies(AuthPolicies.Admin);
        AllowFileUploads();
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        if (!int.TryParse(Route<string>("productId"), out var productId) || productId <= 0)
        {
            ThrowError("Invalid product id.", StatusCodes.Status400BadRequest);
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
            var info = HttpContext.Request.Form["imageInfo"].ToString();
            await using var stream = file.OpenReadStream();
            var image = await _products.UploadProductInfoImageAsync(productId, stream, file.FileName, info, ct);
            HttpContext.Response.StatusCode = StatusCodes.Status201Created;
            await Send.OkAsync(image, ct);
        }
        catch (InvalidOperationException ex)
        {
            ThrowError(ex.Message, StatusCodes.Status400BadRequest);
        }
    }
}

public sealed class UpdateAdminProductInfoImageEndpoint : Endpoint<UpdateProductInfoImageRequest>
{
    private readonly IAdminProductService _products;

    public UpdateAdminProductInfoImageEndpoint(IAdminProductService products) => _products = products;

    public override void Configure()
    {
        Put("admin/products/{productId}/images/{imageId}/info");
        Policies(AuthPolicies.Admin);
    }

    public override async Task HandleAsync(UpdateProductInfoImageRequest req, CancellationToken ct)
    {
        if (!int.TryParse(Route<string>("productId"), out var productId) || productId <= 0
            || !int.TryParse(Route<string>("imageId"), out var imageId) || imageId <= 0)
        {
            ThrowError("Invalid product or image id.", StatusCodes.Status400BadRequest);
            return;
        }

        try
        {
            await _products.UpdateProductInfoImageAsync(productId, imageId, req.ImageInfo, ct);
            await Send.NoContentAsync(ct);
        }
        catch (InvalidOperationException ex)
        {
            ThrowError(ex.Message, StatusCodes.Status400BadRequest);
        }
    }
}

public sealed class ExportAdminProductsRequest
{
    public string Filter { get; init; } = "all";
    public string? Search { get; init; }
}

public sealed class ExportAdminProductsEndpoint : Endpoint<ExportAdminProductsRequest>
{
    private readonly IAdminProductService _products;
    private readonly IDateTimeProvider _clock;

    public ExportAdminProductsEndpoint(IAdminProductService products, IDateTimeProvider clock)
    {
        _products = products;
        _clock = clock;
    }

    public override void Configure()
    {
        Get("admin/products/export-csv");
        Policies(AuthPolicies.Admin);
    }

    public override async Task HandleAsync(ExportAdminProductsRequest req, CancellationToken ct)
    {
        var bytes = await _products.ExportProductsCsvAsync(req.Filter, req.Search, ct);
        var fileName = $"Products_Export_{_clock.Now:yyyyMMdd_HHmmss}.csv";
        HttpContext.Response.ContentType = "text/csv";
        HttpContext.Response.Headers.ContentDisposition = $"attachment; filename=\"{fileName}\"";
        await HttpContext.Response.Body.WriteAsync(bytes, ct);
    }
}

public sealed class RegenerateAdminProductSlugsEndpoint : EndpointWithoutRequest<RegenerateProductSlugsResponse>
{
    private readonly IAdminProductService _products;

    public RegenerateAdminProductSlugsEndpoint(IAdminProductService products) => _products = products;

    public override void Configure()
    {
        Post("admin/products/regenerate-slugs");
        Policies(AuthPolicies.Admin);
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        try
        {
            var result = await _products.RegenerateAllProductSlugsAsync(ct);
            await Send.OkAsync(result, ct);
        }
        catch (Exception ex)
        {
            await Send.OkAsync(new RegenerateProductSlugsResponse
            {
                Success = false,
                Message = $"Error regenerating slugs: {ex.Message}",
                UpdatedCount = 0,
                TotalProducts = 0
            }, ct);
        }
    }
}
