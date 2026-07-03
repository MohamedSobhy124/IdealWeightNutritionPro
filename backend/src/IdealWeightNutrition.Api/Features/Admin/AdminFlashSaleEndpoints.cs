using FastEndpoints;
using IdealWeightNutrition.Application.Abstractions;
using IdealWeightNutrition.Contracts.Admin;
using AuthPolicies = IdealWeightNutrition.Domain.Constants.Policies;

namespace IdealWeightNutrition.Api.Features.Admin;

public sealed class ListAdminFlashSalesRequest
{
    public bool IncludeDeleted { get; init; }
}

public sealed class ListAdminFlashSalesEndpoint : Endpoint<ListAdminFlashSalesRequest, IReadOnlyList<AdminFlashSaleListItemDto>>
{
    private readonly IAdminFlashSaleService _flashSales;

    public ListAdminFlashSalesEndpoint(IAdminFlashSaleService flashSales) => _flashSales = flashSales;

    public override void Configure()
    {
        Get("admin/flash-sales");
        Policies(AuthPolicies.Admin);
    }

    public override async Task HandleAsync(ListAdminFlashSalesRequest req, CancellationToken ct) =>
        await Send.OkAsync(await _flashSales.ListAsync(req.IncludeDeleted, ct), ct);
}

public sealed class GetAdminFlashSaleEndpoint : EndpointWithoutRequest<AdminFlashSaleDetailDto>
{
    private readonly IAdminFlashSaleService _flashSales;

    public GetAdminFlashSaleEndpoint(IAdminFlashSaleService flashSales) => _flashSales = flashSales;

    public override void Configure()
    {
        Get("admin/flash-sales/{flashSaleId}");
        Policies(AuthPolicies.Admin);
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        if (!int.TryParse(Route<string>("flashSaleId"), out var id) || id <= 0)
        {
            ThrowError("Invalid flash sale id.", StatusCodes.Status400BadRequest);
            return;
        }

        var sale = await _flashSales.GetAsync(id, ct);
        if (sale is null)
            ThrowError("Flash sale not found.", StatusCodes.Status404NotFound);
        else
            await Send.OkAsync(sale, ct);
    }
}

public sealed class CreateAdminFlashSaleEndpoint : Endpoint<UpsertAdminFlashSaleRequest, AdminFlashSaleDetailDto>
{
    private readonly IAdminFlashSaleService _flashSales;

    public CreateAdminFlashSaleEndpoint(IAdminFlashSaleService flashSales) => _flashSales = flashSales;

    public override void Configure()
    {
        Post("admin/flash-sales");
        Policies(AuthPolicies.Admin);
    }

    public override async Task HandleAsync(UpsertAdminFlashSaleRequest req, CancellationToken ct)
    {
        try
        {
            var created = await _flashSales.CreateAsync(req, ct);
            HttpContext.Response.StatusCode = StatusCodes.Status201Created;
            await Send.OkAsync(created, ct);
        }
        catch (InvalidOperationException ex)
        {
            ThrowError(ex.Message, StatusCodes.Status400BadRequest);
        }
    }
}

public sealed class UpdateAdminFlashSaleEndpoint : Endpoint<UpsertAdminFlashSaleRequest, AdminFlashSaleDetailDto>
{
    private readonly IAdminFlashSaleService _flashSales;

    public UpdateAdminFlashSaleEndpoint(IAdminFlashSaleService flashSales) => _flashSales = flashSales;

    public override void Configure()
    {
        Put("admin/flash-sales/{flashSaleId}");
        Policies(AuthPolicies.Admin);
    }

    public override async Task HandleAsync(UpsertAdminFlashSaleRequest req, CancellationToken ct)
    {
        if (!int.TryParse(Route<string>("flashSaleId"), out var id) || id <= 0)
        {
            ThrowError("Invalid flash sale id.", StatusCodes.Status400BadRequest);
            return;
        }

        try
        {
            await Send.OkAsync(await _flashSales.UpdateAsync(id, req, ct), ct);
        }
        catch (InvalidOperationException ex)
        {
            ThrowError(ex.Message, StatusCodes.Status400BadRequest);
        }
    }
}

public sealed class ToggleAdminFlashSaleEndpoint : EndpointWithoutRequest
{
    private readonly IAdminFlashSaleService _flashSales;

    public ToggleAdminFlashSaleEndpoint(IAdminFlashSaleService flashSales) => _flashSales = flashSales;

    public override void Configure()
    {
        Post("admin/flash-sales/{flashSaleId}/toggle");
        Policies(AuthPolicies.Admin);
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        if (!int.TryParse(Route<string>("flashSaleId"), out var id) || id <= 0)
        {
            ThrowError("Invalid flash sale id.", StatusCodes.Status400BadRequest);
            return;
        }

        try
        {
            await _flashSales.ToggleActiveAsync(id, ct);
            await Send.NoContentAsync(ct);
        }
        catch (InvalidOperationException ex)
        {
            ThrowError(ex.Message, StatusCodes.Status404NotFound);
        }
    }
}

public sealed class DeleteAdminFlashSaleEndpoint : EndpointWithoutRequest
{
    private readonly IAdminFlashSaleService _flashSales;

    public DeleteAdminFlashSaleEndpoint(IAdminFlashSaleService flashSales) => _flashSales = flashSales;

    public override void Configure()
    {
        Delete("admin/flash-sales/{flashSaleId}");
        Policies(AuthPolicies.Admin);
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        if (!int.TryParse(Route<string>("flashSaleId"), out var id) || id <= 0)
        {
            ThrowError("Invalid flash sale id.", StatusCodes.Status400BadRequest);
            return;
        }

        try
        {
            await _flashSales.SoftDeleteAsync(id, ct);
            await Send.NoContentAsync(ct);
        }
        catch (InvalidOperationException ex)
        {
            ThrowError(ex.Message, StatusCodes.Status404NotFound);
        }
    }
}

public sealed class AddAdminFlashSaleItemEndpoint : Endpoint<AddAdminFlashSaleItemRequest, AdminFlashSaleItemDto>
{
    private readonly IAdminFlashSaleService _flashSales;

    public AddAdminFlashSaleItemEndpoint(IAdminFlashSaleService flashSales) => _flashSales = flashSales;

    public override void Configure()
    {
        Post("admin/flash-sales/{flashSaleId}/items");
        Policies(AuthPolicies.Admin);
    }

    public override async Task HandleAsync(AddAdminFlashSaleItemRequest req, CancellationToken ct)
    {
        if (!int.TryParse(Route<string>("flashSaleId"), out var id) || id <= 0)
        {
            ThrowError("Invalid flash sale id.", StatusCodes.Status400BadRequest);
            return;
        }

        try
        {
            var item = await _flashSales.AddItemAsync(id, req, ct);
            HttpContext.Response.StatusCode = StatusCodes.Status201Created;
            await Send.OkAsync(item, ct);
        }
        catch (InvalidOperationException ex)
        {
            ThrowError(ex.Message, StatusCodes.Status400BadRequest);
        }
    }
}

public sealed class RemoveAdminFlashSaleItemEndpoint : EndpointWithoutRequest
{
    private readonly IAdminFlashSaleService _flashSales;

    public RemoveAdminFlashSaleItemEndpoint(IAdminFlashSaleService flashSales) => _flashSales = flashSales;

    public override void Configure()
    {
        Delete("admin/flash-sales/items/{itemId}");
        Policies(AuthPolicies.Admin);
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        if (!int.TryParse(Route<string>("itemId"), out var id) || id <= 0)
        {
            ThrowError("Invalid item id.", StatusCodes.Status400BadRequest);
            return;
        }

        try
        {
            await _flashSales.RemoveItemAsync(id, ct);
            await Send.NoContentAsync(ct);
        }
        catch (InvalidOperationException ex)
        {
            ThrowError(ex.Message, StatusCodes.Status404NotFound);
        }
    }
}
