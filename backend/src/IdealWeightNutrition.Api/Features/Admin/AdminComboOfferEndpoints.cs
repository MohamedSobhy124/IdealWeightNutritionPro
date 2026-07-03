using FastEndpoints;
using IdealWeightNutrition.Application.Abstractions;
using IdealWeightNutrition.Contracts.Admin;
using AuthPolicies = IdealWeightNutrition.Domain.Constants.Policies;

namespace IdealWeightNutrition.Api.Features.Admin;

public sealed class ListAdminComboOffersRequest
{
    public bool IncludeDeleted { get; init; }
}

public sealed class ListAdminComboOffersEndpoint : Endpoint<ListAdminComboOffersRequest, IReadOnlyList<AdminComboOfferListItemDto>>
{
    private readonly IAdminComboOfferService _combos;

    public ListAdminComboOffersEndpoint(IAdminComboOfferService combos) => _combos = combos;

    public override void Configure()
    {
        Get("admin/combo-offers");
        Policies(AuthPolicies.Admin);
    }

    public override async Task HandleAsync(ListAdminComboOffersRequest req, CancellationToken ct) =>
        await Send.OkAsync(await _combos.ListAsync(req.IncludeDeleted, ct), ct);
}

public sealed class GetAdminComboOfferEndpoint : EndpointWithoutRequest<AdminComboOfferDetailDto>
{
    private readonly IAdminComboOfferService _combos;

    public GetAdminComboOfferEndpoint(IAdminComboOfferService combos) => _combos = combos;

    public override void Configure()
    {
        Get("admin/combo-offers/{comboOfferId}");
        Policies(AuthPolicies.Admin);
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        if (!int.TryParse(Route<string>("comboOfferId"), out var id) || id <= 0)
        {
            ThrowError("Invalid combo offer id.", StatusCodes.Status400BadRequest);
            return;
        }

        var combo = await _combos.GetAsync(id, ct);
        if (combo is null)
            ThrowError("Combo offer not found.", StatusCodes.Status404NotFound);
        else
            await Send.OkAsync(combo, ct);
    }
}

public sealed class CreateAdminComboOfferEndpoint : Endpoint<UpsertAdminComboOfferRequest, AdminComboOfferDetailDto>
{
    private readonly IAdminComboOfferService _combos;

    public CreateAdminComboOfferEndpoint(IAdminComboOfferService combos) => _combos = combos;

    public override void Configure()
    {
        Post("admin/combo-offers");
        Policies(AuthPolicies.Admin);
    }

    public override async Task HandleAsync(UpsertAdminComboOfferRequest req, CancellationToken ct)
    {
        try
        {
            var created = await _combos.CreateAsync(req, ct);
            HttpContext.Response.StatusCode = StatusCodes.Status201Created;
            await Send.OkAsync(created, ct);
        }
        catch (InvalidOperationException ex)
        {
            ThrowError(ex.Message, StatusCodes.Status400BadRequest);
        }
    }
}

public sealed class UpdateAdminComboOfferEndpoint : Endpoint<UpsertAdminComboOfferRequest, AdminComboOfferDetailDto>
{
    private readonly IAdminComboOfferService _combos;

    public UpdateAdminComboOfferEndpoint(IAdminComboOfferService combos) => _combos = combos;

    public override void Configure()
    {
        Put("admin/combo-offers/{comboOfferId}");
        Policies(AuthPolicies.Admin);
    }

    public override async Task HandleAsync(UpsertAdminComboOfferRequest req, CancellationToken ct)
    {
        if (!int.TryParse(Route<string>("comboOfferId"), out var id) || id <= 0)
        {
            ThrowError("Invalid combo offer id.", StatusCodes.Status400BadRequest);
            return;
        }

        try
        {
            await Send.OkAsync(await _combos.UpdateAsync(id, req, ct), ct);
        }
        catch (InvalidOperationException ex)
        {
            ThrowError(ex.Message, StatusCodes.Status400BadRequest);
        }
    }
}

public sealed class ToggleAdminComboOfferEndpoint : EndpointWithoutRequest
{
    private readonly IAdminComboOfferService _combos;

    public ToggleAdminComboOfferEndpoint(IAdminComboOfferService combos) => _combos = combos;

    public override void Configure()
    {
        Post("admin/combo-offers/{comboOfferId}/toggle");
        Policies(AuthPolicies.Admin);
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        if (!int.TryParse(Route<string>("comboOfferId"), out var id) || id <= 0)
        {
            ThrowError("Invalid combo offer id.", StatusCodes.Status400BadRequest);
            return;
        }

        try
        {
            await _combos.ToggleActiveAsync(id, ct);
            await Send.NoContentAsync(ct);
        }
        catch (InvalidOperationException ex)
        {
            ThrowError(ex.Message, StatusCodes.Status404NotFound);
        }
    }
}

public sealed class DeleteAdminComboOfferEndpoint : EndpointWithoutRequest
{
    private readonly IAdminComboOfferService _combos;

    public DeleteAdminComboOfferEndpoint(IAdminComboOfferService combos) => _combos = combos;

    public override void Configure()
    {
        Delete("admin/combo-offers/{comboOfferId}");
        Policies(AuthPolicies.Admin);
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        if (!int.TryParse(Route<string>("comboOfferId"), out var id) || id <= 0)
        {
            ThrowError("Invalid combo offer id.", StatusCodes.Status400BadRequest);
            return;
        }

        try
        {
            await _combos.SoftDeleteAsync(id, ct);
            await Send.NoContentAsync(ct);
        }
        catch (InvalidOperationException ex)
        {
            ThrowError(ex.Message, StatusCodes.Status404NotFound);
        }
    }
}

public sealed class AddAdminComboOfferItemEndpoint : Endpoint<AddAdminComboOfferItemRequest, AdminComboOfferItemDto>
{
    private readonly IAdminComboOfferService _combos;

    public AddAdminComboOfferItemEndpoint(IAdminComboOfferService combos) => _combos = combos;

    public override void Configure()
    {
        Post("admin/combo-offers/{comboOfferId}/items");
        Policies(AuthPolicies.Admin);
    }

    public override async Task HandleAsync(AddAdminComboOfferItemRequest req, CancellationToken ct)
    {
        if (!int.TryParse(Route<string>("comboOfferId"), out var id) || id <= 0)
        {
            ThrowError("Invalid combo offer id.", StatusCodes.Status400BadRequest);
            return;
        }

        try
        {
            var item = await _combos.AddItemAsync(id, req, ct);
            HttpContext.Response.StatusCode = StatusCodes.Status201Created;
            await Send.OkAsync(item, ct);
        }
        catch (InvalidOperationException ex)
        {
            ThrowError(ex.Message, StatusCodes.Status400BadRequest);
        }
    }
}

public sealed class RemoveAdminComboOfferItemEndpoint : EndpointWithoutRequest
{
    private readonly IAdminComboOfferService _combos;

    public RemoveAdminComboOfferItemEndpoint(IAdminComboOfferService combos) => _combos = combos;

    public override void Configure()
    {
        Delete("admin/combo-offers/items/{itemId}");
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
            await _combos.RemoveItemAsync(id, ct);
            await Send.NoContentAsync(ct);
        }
        catch (InvalidOperationException ex)
        {
            ThrowError(ex.Message, StatusCodes.Status404NotFound);
        }
    }
}
