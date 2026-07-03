using FastEndpoints;
using IdealWeightNutrition.Application.Abstractions;
using IdealWeightNutrition.Contracts.Admin;
using AuthPolicies = IdealWeightNutrition.Domain.Constants.Policies;

namespace IdealWeightNutrition.Api.Features.Admin;

public sealed class ListAdminServiceOffersRequest
{
    public int? ServiceSubscriptionId { get; init; }
}

public sealed class ListAdminServiceOffersEndpoint : Endpoint<ListAdminServiceOffersRequest, IReadOnlyList<AdminServiceOfferListItemDto>>
{
    private readonly IAdminServiceOfferService _offers;

    public ListAdminServiceOffersEndpoint(IAdminServiceOfferService offers) => _offers = offers;

    public override void Configure()
    {
        Get("admin/service-offers");
        Policies(AuthPolicies.Admin);
    }

    public override async Task HandleAsync(ListAdminServiceOffersRequest req, CancellationToken ct) =>
        await Send.OkAsync(await _offers.ListAsync(req.ServiceSubscriptionId, ct), ct);
}

public sealed class GetAdminServiceOfferEndpoint : EndpointWithoutRequest<AdminServiceOfferDetailDto>
{
    private readonly IAdminServiceOfferService _offers;

    public GetAdminServiceOfferEndpoint(IAdminServiceOfferService offers) => _offers = offers;

    public override void Configure()
    {
        Get("admin/service-offers/{offerId}");
        Policies(AuthPolicies.Admin);
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        if (!int.TryParse(Route<string>("offerId"), out var id) || id <= 0)
        {
            ThrowError("Invalid offer id.", StatusCodes.Status400BadRequest);
            return;
        }

        var offer = await _offers.GetAsync(id, ct);
        if (offer is null)
            ThrowError("Service offer not found.", StatusCodes.Status404NotFound);
        else
            await Send.OkAsync(offer, ct);
    }
}

public sealed class CreateAdminServiceOfferEndpoint : Endpoint<UpsertAdminServiceOfferRequest, AdminServiceOfferDetailDto>
{
    private readonly IAdminServiceOfferService _offers;

    public CreateAdminServiceOfferEndpoint(IAdminServiceOfferService offers) => _offers = offers;

    public override void Configure()
    {
        Post("admin/service-offers");
        Policies(AuthPolicies.Admin);
    }

    public override async Task HandleAsync(UpsertAdminServiceOfferRequest req, CancellationToken ct)
    {
        try
        {
            var offer = await _offers.CreateAsync(req, ct);
            HttpContext.Response.StatusCode = StatusCodes.Status201Created;
            await Send.OkAsync(offer, ct);
        }
        catch (InvalidOperationException ex)
        {
            ThrowError(ex.Message, StatusCodes.Status400BadRequest);
        }
    }
}

public sealed class UpdateAdminServiceOfferEndpoint : Endpoint<UpsertAdminServiceOfferRequest, AdminServiceOfferDetailDto>
{
    private readonly IAdminServiceOfferService _offers;

    public UpdateAdminServiceOfferEndpoint(IAdminServiceOfferService offers) => _offers = offers;

    public override void Configure()
    {
        Put("admin/service-offers/{offerId}");
        Policies(AuthPolicies.Admin);
    }

    public override async Task HandleAsync(UpsertAdminServiceOfferRequest req, CancellationToken ct)
    {
        if (!int.TryParse(Route<string>("offerId"), out var id) || id <= 0)
        {
            ThrowError("Invalid offer id.", StatusCodes.Status400BadRequest);
            return;
        }

        try
        {
            await Send.OkAsync(await _offers.UpdateAsync(id, req, ct), ct);
        }
        catch (InvalidOperationException ex)
        {
            ThrowError(ex.Message, StatusCodes.Status400BadRequest);
        }
    }
}

public sealed class ToggleAdminServiceOfferEndpoint : EndpointWithoutRequest
{
    private readonly IAdminServiceOfferService _offers;

    public ToggleAdminServiceOfferEndpoint(IAdminServiceOfferService offers) => _offers = offers;

    public override void Configure()
    {
        Post("admin/service-offers/{offerId}/toggle");
        Policies(AuthPolicies.Admin);
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        if (!int.TryParse(Route<string>("offerId"), out var id) || id <= 0)
        {
            ThrowError("Invalid offer id.", StatusCodes.Status400BadRequest);
            return;
        }

        try
        {
            await _offers.ToggleActiveAsync(id, ct);
            await Send.NoContentAsync(ct);
        }
        catch (InvalidOperationException ex)
        {
            ThrowError(ex.Message, StatusCodes.Status404NotFound);
        }
    }
}

public sealed class DeleteAdminServiceOfferEndpoint : EndpointWithoutRequest
{
    private readonly IAdminServiceOfferService _offers;

    public DeleteAdminServiceOfferEndpoint(IAdminServiceOfferService offers) => _offers = offers;

    public override void Configure()
    {
        Delete("admin/service-offers/{offerId}");
        Policies(AuthPolicies.Admin);
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        if (!int.TryParse(Route<string>("offerId"), out var id) || id <= 0)
        {
            ThrowError("Invalid offer id.", StatusCodes.Status400BadRequest);
            return;
        }

        try
        {
            await _offers.DeleteAsync(id, ct);
            await Send.NoContentAsync(ct);
        }
        catch (InvalidOperationException ex)
        {
            ThrowError(ex.Message, StatusCodes.Status404NotFound);
        }
    }
}
