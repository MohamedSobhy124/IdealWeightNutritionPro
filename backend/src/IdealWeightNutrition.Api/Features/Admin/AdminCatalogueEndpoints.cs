using FastEndpoints;
using IdealWeightNutrition.Application.Abstractions;
using IdealWeightNutrition.Contracts.Admin;
using AuthPolicies = IdealWeightNutrition.Domain.Constants.Policies;

namespace IdealWeightNutrition.Api.Features.Admin;

public sealed class ListAdminCategoriesRequest
{
    public bool IncludeDeleted { get; init; }
}

public sealed class ListAdminCategoriesEndpoint : Endpoint<ListAdminCategoriesRequest, IReadOnlyList<AdminCategoryDto>>
{
    private readonly IAdminCatalogueService _catalogue;

    public ListAdminCategoriesEndpoint(IAdminCatalogueService catalogue) => _catalogue = catalogue;

    public override void Configure()
    {
        Get("admin/categories");
        Policies(AuthPolicies.Admin);
    }

    public override async Task HandleAsync(ListAdminCategoriesRequest req, CancellationToken ct) =>
        await Send.OkAsync(await _catalogue.ListCategoriesAsync(req.IncludeDeleted, ct), ct);
}

public sealed class CreateAdminCategoryEndpoint : Endpoint<UpsertAdminCategoryRequest, AdminCategoryDto>
{
    private readonly IAdminCatalogueService _catalogue;

    public CreateAdminCategoryEndpoint(IAdminCatalogueService catalogue) => _catalogue = catalogue;

    public override void Configure()
    {
        Post("admin/categories");
        Policies(AuthPolicies.Admin);
    }

    public override async Task HandleAsync(UpsertAdminCategoryRequest req, CancellationToken ct)
    {
        try
        {
            var created = await _catalogue.CreateCategoryAsync(req, ct);
            HttpContext.Response.StatusCode = StatusCodes.Status201Created;
            await Send.OkAsync(created, ct);
        }
        catch (InvalidOperationException ex)
        {
            ThrowError(ex.Message, StatusCodes.Status400BadRequest);
        }
    }
}

public sealed class UpdateAdminCategoryEndpoint : Endpoint<UpsertAdminCategoryRequest, AdminCategoryDto>
{
    private readonly IAdminCatalogueService _catalogue;

    public UpdateAdminCategoryEndpoint(IAdminCatalogueService catalogue) => _catalogue = catalogue;

    public override void Configure()
    {
        Put("admin/categories/{categoryId}");
        Policies(AuthPolicies.Admin);
    }

    public override async Task HandleAsync(UpsertAdminCategoryRequest req, CancellationToken ct)
    {
        if (!int.TryParse(Route<string>("categoryId"), out var id) || id <= 0)
        {
            ThrowError("Invalid category id.", StatusCodes.Status400BadRequest);
            return;
        }

        try
        {
            await Send.OkAsync(await _catalogue.UpdateCategoryAsync(id, req, ct), ct);
        }
        catch (InvalidOperationException ex)
        {
            ThrowError(ex.Message, StatusCodes.Status400BadRequest);
        }
    }
}

public sealed class DeleteAdminCategoryEndpoint : EndpointWithoutRequest
{
    private readonly IAdminCatalogueService _catalogue;

    public DeleteAdminCategoryEndpoint(IAdminCatalogueService catalogue) => _catalogue = catalogue;

    public override void Configure()
    {
        Delete("admin/categories/{categoryId}");
        Policies(AuthPolicies.Admin);
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        if (!int.TryParse(Route<string>("categoryId"), out var id) || id <= 0)
        {
            ThrowError("Invalid category id.", StatusCodes.Status400BadRequest);
            return;
        }

        try
        {
            await _catalogue.DeleteCategoryAsync(id, ct);
            await Send.NoContentAsync(ct);
        }
        catch (InvalidOperationException ex)
        {
            ThrowError(ex.Message, StatusCodes.Status400BadRequest);
        }
    }
}

public sealed class ListAdminBrandsRequest
{
    public bool IncludeDeleted { get; init; }
}

public sealed class ListAdminBrandsEndpoint : Endpoint<ListAdminBrandsRequest, IReadOnlyList<AdminBrandDto>>
{
    private readonly IAdminCatalogueService _catalogue;

    public ListAdminBrandsEndpoint(IAdminCatalogueService catalogue) => _catalogue = catalogue;

    public override void Configure()
    {
        Get("admin/brands");
        Policies(AuthPolicies.Admin);
    }

    public override async Task HandleAsync(ListAdminBrandsRequest req, CancellationToken ct) =>
        await Send.OkAsync(await _catalogue.ListBrandsAsync(req.IncludeDeleted, ct), ct);
}

public sealed class CreateAdminBrandEndpoint : Endpoint<UpsertAdminBrandRequest, AdminBrandDto>
{
    private readonly IAdminCatalogueService _catalogue;

    public CreateAdminBrandEndpoint(IAdminCatalogueService catalogue) => _catalogue = catalogue;

    public override void Configure()
    {
        Post("admin/brands");
        Policies(AuthPolicies.Admin);
    }

    public override async Task HandleAsync(UpsertAdminBrandRequest req, CancellationToken ct)
    {
        try
        {
            var created = await _catalogue.CreateBrandAsync(req, ct);
            HttpContext.Response.StatusCode = StatusCodes.Status201Created;
            await Send.OkAsync(created, ct);
        }
        catch (InvalidOperationException ex)
        {
            ThrowError(ex.Message, StatusCodes.Status400BadRequest);
        }
    }
}

public sealed class UpdateAdminBrandEndpoint : Endpoint<UpsertAdminBrandRequest, AdminBrandDto>
{
    private readonly IAdminCatalogueService _catalogue;

    public UpdateAdminBrandEndpoint(IAdminCatalogueService catalogue) => _catalogue = catalogue;

    public override void Configure()
    {
        Put("admin/brands/{brandId}");
        Policies(AuthPolicies.Admin);
    }

    public override async Task HandleAsync(UpsertAdminBrandRequest req, CancellationToken ct)
    {
        if (!int.TryParse(Route<string>("brandId"), out var id) || id <= 0)
        {
            ThrowError("Invalid brand id.", StatusCodes.Status400BadRequest);
            return;
        }

        try
        {
            await Send.OkAsync(await _catalogue.UpdateBrandAsync(id, req, ct), ct);
        }
        catch (InvalidOperationException ex)
        {
            ThrowError(ex.Message, StatusCodes.Status400BadRequest);
        }
    }
}

public sealed class DeleteAdminBrandEndpoint : EndpointWithoutRequest
{
    private readonly IAdminCatalogueService _catalogue;

    public DeleteAdminBrandEndpoint(IAdminCatalogueService catalogue) => _catalogue = catalogue;

    public override void Configure()
    {
        Delete("admin/brands/{brandId}");
        Policies(AuthPolicies.Admin);
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        if (!int.TryParse(Route<string>("brandId"), out var id) || id <= 0)
        {
            ThrowError("Invalid brand id.", StatusCodes.Status400BadRequest);
            return;
        }

        try
        {
            await _catalogue.DeleteBrandAsync(id, ct);
            await Send.NoContentAsync(ct);
        }
        catch (InvalidOperationException ex)
        {
            ThrowError(ex.Message, StatusCodes.Status400BadRequest);
        }
    }
}
