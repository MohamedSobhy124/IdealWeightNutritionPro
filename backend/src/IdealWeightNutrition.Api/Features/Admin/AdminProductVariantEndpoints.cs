using FastEndpoints;
using IdealWeightNutrition.Application.Abstractions;
using IdealWeightNutrition.Contracts.Admin;
using AuthPolicies = IdealWeightNutrition.Domain.Constants.Policies;

namespace IdealWeightNutrition.Api.Features.Admin;

public sealed class GetAdminProductOptionsEndpoint : EndpointWithoutRequest<IReadOnlyList<AdminProductOptionDto>>
{
    private readonly IAdminProductVariantService _variants;
    public GetAdminProductOptionsEndpoint(IAdminProductVariantService variants) => _variants = variants;
    public override void Configure() { Get("admin/products/{productId}/options"); Policies(AuthPolicies.Admin); }
    public override async Task HandleAsync(CancellationToken ct)
    {
        if (!int.TryParse(Route<string>("productId"), out var productId) || productId <= 0)
            ThrowError("Invalid product id.", StatusCodes.Status400BadRequest);
        try { await Send.OkAsync(await _variants.GetOptionsAsync(productId, ct), ct); }
        catch (InvalidOperationException ex) { ThrowError(ex.Message, StatusCodes.Status400BadRequest); }
    }
}

public sealed class AddAdminProductOptionEndpoint : Endpoint<AddAdminProductOptionRequest, AdminProductOptionDto>
{
    private readonly IAdminProductVariantService _variants;
    public AddAdminProductOptionEndpoint(IAdminProductVariantService variants) => _variants = variants;
    public override void Configure() { Post("admin/products/{productId}/options"); Policies(AuthPolicies.Admin); }
    public override async Task HandleAsync(AddAdminProductOptionRequest req, CancellationToken ct)
    {
        if (!int.TryParse(Route<string>("productId"), out var productId) || productId <= 0)
            ThrowError("Invalid product id.", StatusCodes.Status400BadRequest);
        try
        {
            var option = await _variants.AddOptionAsync(productId, req, ct);
            HttpContext.Response.StatusCode = StatusCodes.Status201Created;
            await Send.OkAsync(option, ct);
        }
        catch (InvalidOperationException ex) { ThrowError(ex.Message, StatusCodes.Status400BadRequest); }
    }
}

public sealed class DeleteAdminProductOptionEndpoint : EndpointWithoutRequest<AdminActionResponse>
{
    private readonly IAdminProductVariantService _variants;
    public DeleteAdminProductOptionEndpoint(IAdminProductVariantService variants) => _variants = variants;
    public override void Configure() { Delete("admin/products/{productId}/options/{optionId}"); Policies(AuthPolicies.Admin); }
    public override async Task HandleAsync(CancellationToken ct)
    {
        if (!int.TryParse(Route<string>("productId"), out var productId) || productId <= 0
            || !int.TryParse(Route<string>("optionId"), out var optionId) || optionId <= 0)
        {
            ThrowError("Invalid ids.", StatusCodes.Status400BadRequest);
            return;
        }
        try
        {
            await _variants.DeleteOptionAsync(productId, optionId, ct);
            await Send.OkAsync(new AdminActionResponse { Message = "Option deleted." }, ct);
        }
        catch (InvalidOperationException ex) { ThrowError(ex.Message, StatusCodes.Status400BadRequest); }
    }
}

public sealed class AddAdminProductOptionValueEndpoint : Endpoint<AddAdminProductOptionValueRequest, AdminProductOptionValueDto>
{
    private readonly IAdminProductVariantService _variants;
    public AddAdminProductOptionValueEndpoint(IAdminProductVariantService variants) => _variants = variants;
    public override void Configure() { Post("admin/products/{productId}/options/{optionId}/values"); Policies(AuthPolicies.Admin); }
    public override async Task HandleAsync(AddAdminProductOptionValueRequest req, CancellationToken ct)
    {
        if (!int.TryParse(Route<string>("productId"), out var productId) || productId <= 0
            || !int.TryParse(Route<string>("optionId"), out var optionId) || optionId <= 0)
        {
            ThrowError("Invalid ids.", StatusCodes.Status400BadRequest);
            return;
        }
        try
        {
            var value = await _variants.AddOptionValueAsync(productId, optionId, req, ct);
            HttpContext.Response.StatusCode = StatusCodes.Status201Created;
            await Send.OkAsync(value, ct);
        }
        catch (InvalidOperationException ex) { ThrowError(ex.Message, StatusCodes.Status400BadRequest); }
    }
}

public sealed class DeleteAdminProductOptionValueEndpoint : EndpointWithoutRequest<AdminActionResponse>
{
    private readonly IAdminProductVariantService _variants;
    public DeleteAdminProductOptionValueEndpoint(IAdminProductVariantService variants) => _variants = variants;
    public override void Configure() { Delete("admin/products/{productId}/option-values/{valueId}"); Policies(AuthPolicies.Admin); }
    public override async Task HandleAsync(CancellationToken ct)
    {
        if (!int.TryParse(Route<string>("productId"), out var productId) || productId <= 0
            || !int.TryParse(Route<string>("valueId"), out var valueId) || valueId <= 0)
        {
            ThrowError("Invalid ids.", StatusCodes.Status400BadRequest);
            return;
        }
        try
        {
            await _variants.DeleteOptionValueAsync(productId, valueId, ct);
            await Send.OkAsync(new AdminActionResponse { Message = "Option value deleted." }, ct);
        }
        catch (InvalidOperationException ex) { ThrowError(ex.Message, StatusCodes.Status400BadRequest); }
    }
}

public sealed class GenerateAdminProductVariantsEndpoint : EndpointWithoutRequest<GenerateVariantsResponse>
{
    private readonly IAdminProductVariantService _variants;
    public GenerateAdminProductVariantsEndpoint(IAdminProductVariantService variants) => _variants = variants;
    public override void Configure() { Post("admin/products/{productId}/variants/generate"); Policies(AuthPolicies.Admin); }
    public override async Task HandleAsync(CancellationToken ct)
    {
        if (!int.TryParse(Route<string>("productId"), out var productId) || productId <= 0)
            ThrowError("Invalid product id.", StatusCodes.Status400BadRequest);
        try { await Send.OkAsync(await _variants.GenerateVariantsAsync(productId, ct), ct); }
        catch (InvalidOperationException ex) { ThrowError(ex.Message, StatusCodes.Status400BadRequest); }
    }
}

public sealed class UpdateAdminProductVariantEndpoint : Endpoint<UpdateAdminProductVariantDetailRequest, AdminProductVariantDto>
{
    private readonly IAdminProductVariantService _variants;
    public UpdateAdminProductVariantEndpoint(IAdminProductVariantService variants) => _variants = variants;
    public override void Configure() { Put("admin/products/{productId}/variants/{variantId}"); Policies(AuthPolicies.Admin); }
    public override async Task HandleAsync(UpdateAdminProductVariantDetailRequest req, CancellationToken ct)
    {
        if (!int.TryParse(Route<string>("productId"), out var productId) || productId <= 0
            || !int.TryParse(Route<string>("variantId"), out var variantId) || variantId <= 0)
        {
            ThrowError("Invalid ids.", StatusCodes.Status400BadRequest);
            return;
        }
        try { await Send.OkAsync(await _variants.UpdateVariantAsync(productId, variantId, req, ct), ct); }
        catch (InvalidOperationException ex) { ThrowError(ex.Message, StatusCodes.Status400BadRequest); }
    }
}

public sealed class UploadAdminProductVariantImageEndpoint : EndpointWithoutRequest<AdminImageUploadResultDto>
{
    private readonly IAdminProductVariantService _variants;

    public UploadAdminProductVariantImageEndpoint(IAdminProductVariantService variants) => _variants = variants;

    public override void Configure()
    {
        Post("admin/products/{productId}/variants/{variantId}/image");
        Policies(AuthPolicies.Admin);
        AllowFileUploads();
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        if (!int.TryParse(Route<string>("productId"), out var productId) || productId <= 0
            || !int.TryParse(Route<string>("variantId"), out var variantId) || variantId <= 0)
        {
            ThrowError("Invalid ids.", StatusCodes.Status400BadRequest);
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
            var result = await _variants.UploadVariantImageAsync(productId, variantId, stream, file.FileName, ct);
            HttpContext.Response.StatusCode = StatusCodes.Status201Created;
            await Send.OkAsync(result, ct);
        }
        catch (InvalidOperationException ex)
        {
            ThrowError(ex.Message, StatusCodes.Status400BadRequest);
        }
    }
}

public sealed class SetAdminProductTypeEndpoint : Endpoint<SetProductTypeRequest, AdminActionResponse>
{
    private readonly IAdminProductVariantService _variants;
    public SetAdminProductTypeEndpoint(IAdminProductVariantService variants) => _variants = variants;
    public override void Configure() { Put("admin/products/{productId}/product-type"); Policies(AuthPolicies.Admin); }
    public override async Task HandleAsync(SetProductTypeRequest req, CancellationToken ct)
    {
        if (!int.TryParse(Route<string>("productId"), out var productId) || productId <= 0)
            ThrowError("Invalid product id.", StatusCodes.Status400BadRequest);
        try
        {
            await _variants.SetProductTypeAsync(productId, req.ProductType, ct);
            await Send.OkAsync(new AdminActionResponse { Message = "Product type updated." }, ct);
        }
        catch (InvalidOperationException ex) { ThrowError(ex.Message, StatusCodes.Status400BadRequest); }
    }
}
