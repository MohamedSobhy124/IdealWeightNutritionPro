using FastEndpoints;
using IdealWeightNutrition.Api.Http;
using IdealWeightNutrition.Application.Abstractions;
using IdealWeightNutrition.Contracts.Admin;
using AuthPolicies = IdealWeightNutrition.Domain.Constants.Policies;

namespace IdealWeightNutrition.Api.Features.Admin;

public sealed class ListAdminPromoCodesEndpoint : EndpointWithoutRequest<IReadOnlyList<AdminPromoCodeListItemDto>>
{
    private readonly IAdminPromoCodeService _promos;

    public ListAdminPromoCodesEndpoint(IAdminPromoCodeService promos) => _promos = promos;

    public override void Configure()
    {
        Get("admin/promo-codes");
        Policies(AuthPolicies.Admin);
    }

    public override async Task HandleAsync(CancellationToken ct) =>
        await Send.OkAsync(await _promos.ListAsync(ct), ct);
}

public sealed class GetAdminPromoCodeEndpoint : EndpointWithoutRequest<AdminPromoCodeDetailDto>
{
    private readonly IAdminPromoCodeService _promos;

    public GetAdminPromoCodeEndpoint(IAdminPromoCodeService promos) => _promos = promos;

    public override void Configure()
    {
        Get("admin/promo-codes/{promoId}");
        Policies(AuthPolicies.Admin);
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        if (!int.TryParse(Route<string>("promoId"), out var id) || id <= 0)
        {
            ThrowError("Invalid promo id.", StatusCodes.Status400BadRequest);
            return;
        }

        var promo = await _promos.GetAsync(id, ct);
        if (promo is null)
            ThrowError("Promo code not found.", StatusCodes.Status404NotFound);
        else
            await Send.OkAsync(promo, ct);
    }
}

public sealed class CreateAdminPromoCodeEndpoint : Endpoint<UpsertAdminPromoCodeRequest, AdminPromoCodeDetailDto>
{
    private readonly IAdminPromoCodeService _promos;

    public CreateAdminPromoCodeEndpoint(IAdminPromoCodeService promos) => _promos = promos;

    public override void Configure()
    {
        Post("admin/promo-codes");
        Policies(AuthPolicies.Admin);
    }

    public override async Task HandleAsync(UpsertAdminPromoCodeRequest req, CancellationToken ct)
    {
        try
        {
            var userId = CartHttp.GetUserId(User);
            var created = await _promos.CreateAsync(req, userId, ct);
            HttpContext.Response.StatusCode = StatusCodes.Status201Created;
            await Send.OkAsync(created, ct);
        }
        catch (InvalidOperationException ex)
        {
            ThrowError(ex.Message, StatusCodes.Status400BadRequest);
        }
    }
}

public sealed class UpdateAdminPromoCodeEndpoint : Endpoint<UpsertAdminPromoCodeRequest, AdminPromoCodeDetailDto>
{
    private readonly IAdminPromoCodeService _promos;

    public UpdateAdminPromoCodeEndpoint(IAdminPromoCodeService promos) => _promos = promos;

    public override void Configure()
    {
        Put("admin/promo-codes/{promoId}");
        Policies(AuthPolicies.Admin);
    }

    public override async Task HandleAsync(UpsertAdminPromoCodeRequest req, CancellationToken ct)
    {
        if (!int.TryParse(Route<string>("promoId"), out var id) || id <= 0)
        {
            ThrowError("Invalid promo id.", StatusCodes.Status400BadRequest);
            return;
        }

        try
        {
            await Send.OkAsync(await _promos.UpdateAsync(id, req, ct), ct);
        }
        catch (InvalidOperationException ex)
        {
            ThrowError(ex.Message, StatusCodes.Status400BadRequest);
        }
    }
}

public sealed class ToggleAdminPromoCodeEndpoint : EndpointWithoutRequest
{
    private readonly IAdminPromoCodeService _promos;

    public ToggleAdminPromoCodeEndpoint(IAdminPromoCodeService promos) => _promos = promos;

    public override void Configure()
    {
        Post("admin/promo-codes/{promoId}/toggle");
        Policies(AuthPolicies.Admin);
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        if (!int.TryParse(Route<string>("promoId"), out var id) || id <= 0)
        {
            ThrowError("Invalid promo id.", StatusCodes.Status400BadRequest);
            return;
        }

        try
        {
            await _promos.ToggleActiveAsync(id, ct);
            await Send.NoContentAsync(ct);
        }
        catch (InvalidOperationException ex)
        {
            ThrowError(ex.Message, StatusCodes.Status404NotFound);
        }
    }
}

public sealed class DeleteAdminPromoCodeEndpoint : EndpointWithoutRequest
{
    private readonly IAdminPromoCodeService _promos;

    public DeleteAdminPromoCodeEndpoint(IAdminPromoCodeService promos) => _promos = promos;

    public override void Configure()
    {
        Delete("admin/promo-codes/{promoId}");
        Policies(AuthPolicies.Admin);
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        if (!int.TryParse(Route<string>("promoId"), out var id) || id <= 0)
        {
            ThrowError("Invalid promo id.", StatusCodes.Status400BadRequest);
            return;
        }

        try
        {
            await _promos.DeleteAsync(id, ct);
            await Send.NoContentAsync(ct);
        }
        catch (InvalidOperationException ex)
        {
            ThrowError(ex.Message, StatusCodes.Status400BadRequest);
        }
    }
}

public sealed class GetAdminPromoExclusionsEndpoint : EndpointWithoutRequest<PromoCodeExclusionsDto>
{
    private readonly IAdminPromoCodeService _promos;

    public GetAdminPromoExclusionsEndpoint(IAdminPromoCodeService promos) => _promos = promos;

    public override void Configure()
    {
        Get("admin/promo-codes/{promoId}/exclusions");
        Policies(AuthPolicies.Admin);
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        if (!int.TryParse(Route<string>("promoId"), out var id) || id <= 0)
        {
            ThrowError("Invalid promo id.", StatusCodes.Status400BadRequest);
            return;
        }

        try
        {
            await Send.OkAsync(await _promos.GetExclusionsAsync(id, ct), ct);
        }
        catch (InvalidOperationException ex)
        {
            ThrowError(ex.Message, StatusCodes.Status404NotFound);
        }
    }
}

public sealed class AddAdminPromoExcludedProductRequest
{
    public int ProductId { get; init; }
}

public sealed class AddAdminPromoExcludedProductEndpoint : Endpoint<AddAdminPromoExcludedProductRequest>
{
    private readonly IAdminPromoCodeService _promos;

    public AddAdminPromoExcludedProductEndpoint(IAdminPromoCodeService promos) => _promos = promos;

    public override void Configure()
    {
        Post("admin/promo-codes/{promoId}/exclusions/products");
        Policies(AuthPolicies.Admin);
    }

    public override async Task HandleAsync(AddAdminPromoExcludedProductRequest req, CancellationToken ct)
    {
        if (!int.TryParse(Route<string>("promoId"), out var id) || id <= 0)
        {
            ThrowError("Invalid promo id.", StatusCodes.Status400BadRequest);
            return;
        }

        try
        {
            await _promos.AddExcludedProductAsync(id, req.ProductId, ct);
            await Send.NoContentAsync(ct);
        }
        catch (InvalidOperationException ex)
        {
            ThrowError(ex.Message, StatusCodes.Status400BadRequest);
        }
    }
}

public sealed class RemoveAdminPromoExcludedProductEndpoint : EndpointWithoutRequest
{
    private readonly IAdminPromoCodeService _promos;

    public RemoveAdminPromoExcludedProductEndpoint(IAdminPromoCodeService promos) => _promos = promos;

    public override void Configure()
    {
        Delete("admin/promo-codes/exclusions/products/{exclusionId}");
        Policies(AuthPolicies.Admin);
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        if (!int.TryParse(Route<string>("exclusionId"), out var id) || id <= 0)
        {
            ThrowError("Invalid exclusion id.", StatusCodes.Status400BadRequest);
            return;
        }

        try
        {
            await _promos.RemoveExcludedProductAsync(id, ct);
            await Send.NoContentAsync(ct);
        }
        catch (InvalidOperationException ex)
        {
            ThrowError(ex.Message, StatusCodes.Status404NotFound);
        }
    }
}

public sealed class AddAdminPromoExcludedComboRequest
{
    public int ComboOfferId { get; init; }
}

public sealed class AddAdminPromoExcludedComboEndpoint : Endpoint<AddAdminPromoExcludedComboRequest>
{
    private readonly IAdminPromoCodeService _promos;

    public AddAdminPromoExcludedComboEndpoint(IAdminPromoCodeService promos) => _promos = promos;

    public override void Configure()
    {
        Post("admin/promo-codes/{promoId}/exclusions/combos");
        Policies(AuthPolicies.Admin);
    }

    public override async Task HandleAsync(AddAdminPromoExcludedComboRequest req, CancellationToken ct)
    {
        if (!int.TryParse(Route<string>("promoId"), out var id) || id <= 0)
        {
            ThrowError("Invalid promo id.", StatusCodes.Status400BadRequest);
            return;
        }

        try
        {
            await _promos.AddExcludedComboOfferAsync(id, req.ComboOfferId, ct);
            await Send.NoContentAsync(ct);
        }
        catch (InvalidOperationException ex)
        {
            ThrowError(ex.Message, StatusCodes.Status400BadRequest);
        }
    }
}

public sealed class RemoveAdminPromoExcludedComboEndpoint : EndpointWithoutRequest
{
    private readonly IAdminPromoCodeService _promos;

    public RemoveAdminPromoExcludedComboEndpoint(IAdminPromoCodeService promos) => _promos = promos;

    public override void Configure()
    {
        Delete("admin/promo-codes/exclusions/combos/{exclusionId}");
        Policies(AuthPolicies.Admin);
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        if (!int.TryParse(Route<string>("exclusionId"), out var id) || id <= 0)
        {
            ThrowError("Invalid exclusion id.", StatusCodes.Status400BadRequest);
            return;
        }

        try
        {
            await _promos.RemoveExcludedComboOfferAsync(id, ct);
            await Send.NoContentAsync(ct);
        }
        catch (InvalidOperationException ex)
        {
            ThrowError(ex.Message, StatusCodes.Status404NotFound);
        }
    }
}

public sealed class AddAdminPromoExcludedServiceRequest
{
    public int ServiceSubscriptionId { get; init; }
}

public sealed class AddAdminPromoExcludedServiceEndpoint : Endpoint<AddAdminPromoExcludedServiceRequest>
{
    private readonly IAdminPromoCodeService _promos;

    public AddAdminPromoExcludedServiceEndpoint(IAdminPromoCodeService promos) => _promos = promos;

    public override void Configure()
    {
        Post("admin/promo-codes/{promoId}/exclusions/services");
        Policies(AuthPolicies.Admin);
    }

    public override async Task HandleAsync(AddAdminPromoExcludedServiceRequest req, CancellationToken ct)
    {
        if (!int.TryParse(Route<string>("promoId"), out var id) || id <= 0)
        {
            ThrowError("Invalid promo id.", StatusCodes.Status400BadRequest);
            return;
        }

        try
        {
            await _promos.AddExcludedServiceAsync(id, req.ServiceSubscriptionId, ct);
            await Send.NoContentAsync(ct);
        }
        catch (InvalidOperationException ex)
        {
            ThrowError(ex.Message, StatusCodes.Status400BadRequest);
        }
    }
}

public sealed class RemoveAdminPromoExcludedServiceEndpoint : EndpointWithoutRequest
{
    private readonly IAdminPromoCodeService _promos;

    public RemoveAdminPromoExcludedServiceEndpoint(IAdminPromoCodeService promos) => _promos = promos;

    public override void Configure()
    {
        Delete("admin/promo-codes/exclusions/services/{exclusionId}");
        Policies(AuthPolicies.Admin);
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        if (!int.TryParse(Route<string>("exclusionId"), out var id) || id <= 0)
        {
            ThrowError("Invalid exclusion id.", StatusCodes.Status400BadRequest);
            return;
        }

        try
        {
            await _promos.RemoveExcludedServiceAsync(id, ct);
            await Send.NoContentAsync(ct);
        }
        catch (InvalidOperationException ex)
        {
            ThrowError(ex.Message, StatusCodes.Status404NotFound);
        }
    }
}
