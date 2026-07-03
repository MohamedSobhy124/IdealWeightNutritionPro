using FastEndpoints;
using IdealWeightNutrition.Application.Abstractions;
using IdealWeightNutrition.Contracts.Admin;
using AuthPolicies = IdealWeightNutrition.Domain.Constants.Policies;

namespace IdealWeightNutrition.Api.Features.Admin;

public sealed class ListAdminCitiesEndpoint : EndpointWithoutRequest<IReadOnlyList<AdminCityDto>>
{
    private readonly IAdminDeliveryService _delivery;

    public ListAdminCitiesEndpoint(IAdminDeliveryService delivery) => _delivery = delivery;

    public override void Configure()
    {
        Get("admin/cities");
        Policies(AuthPolicies.Admin);
    }

    public override async Task HandleAsync(CancellationToken ct) =>
        await Send.OkAsync(await _delivery.ListCitiesAsync(ct), ct);
}

public sealed class CreateAdminCityEndpoint : Endpoint<UpsertAdminCityRequest, AdminCityDto>
{
    private readonly IAdminDeliveryService _delivery;

    public CreateAdminCityEndpoint(IAdminDeliveryService delivery) => _delivery = delivery;

    public override void Configure()
    {
        Post("admin/cities");
        Policies(AuthPolicies.Admin);
    }

    public override async Task HandleAsync(UpsertAdminCityRequest req, CancellationToken ct)
    {
        try
        {
            var created = await _delivery.CreateCityAsync(req, ct);
            HttpContext.Response.StatusCode = StatusCodes.Status201Created;
            await Send.OkAsync(created, ct);
        }
        catch (InvalidOperationException ex)
        {
            ThrowError(ex.Message, StatusCodes.Status400BadRequest);
        }
    }
}

public sealed class UpdateAdminCityEndpoint : Endpoint<UpsertAdminCityRequest, AdminCityDto>
{
    private readonly IAdminDeliveryService _delivery;

    public UpdateAdminCityEndpoint(IAdminDeliveryService delivery) => _delivery = delivery;

    public override void Configure()
    {
        Put("admin/cities/{cityId}");
        Policies(AuthPolicies.Admin);
    }

    public override async Task HandleAsync(UpsertAdminCityRequest req, CancellationToken ct)
    {
        if (!int.TryParse(Route<string>("cityId"), out var id) || id <= 0)
        {
            ThrowError("Invalid city id.", StatusCodes.Status400BadRequest);
            return;
        }

        try
        {
            await Send.OkAsync(await _delivery.UpdateCityAsync(id, req, ct), ct);
        }
        catch (InvalidOperationException ex)
        {
            ThrowError(ex.Message, StatusCodes.Status400BadRequest);
        }
    }
}

public sealed class DeleteAdminCityEndpoint : EndpointWithoutRequest
{
    private readonly IAdminDeliveryService _delivery;

    public DeleteAdminCityEndpoint(IAdminDeliveryService delivery) => _delivery = delivery;

    public override void Configure()
    {
        Delete("admin/cities/{cityId}");
        Policies(AuthPolicies.Admin);
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        if (!int.TryParse(Route<string>("cityId"), out var id) || id <= 0)
        {
            ThrowError("Invalid city id.", StatusCodes.Status400BadRequest);
            return;
        }

        try
        {
            await _delivery.DeleteCityAsync(id, ct);
            await Send.NoContentAsync(ct);
        }
        catch (InvalidOperationException ex)
        {
            ThrowError(ex.Message, StatusCodes.Status400BadRequest);
        }
    }
}

public sealed class ListAdminRemoteAreasEndpoint : EndpointWithoutRequest<IReadOnlyList<AdminRemoteAreaDto>>
{
    private readonly IAdminDeliveryService _delivery;

    public ListAdminRemoteAreasEndpoint(IAdminDeliveryService delivery) => _delivery = delivery;

    public override void Configure()
    {
        Get("admin/cities/{cityId}/remote-areas");
        Policies(AuthPolicies.Admin);
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        if (!int.TryParse(Route<string>("cityId"), out var id) || id <= 0)
        {
            ThrowError("Invalid city id.", StatusCodes.Status400BadRequest);
            return;
        }

        try
        {
            await Send.OkAsync(await _delivery.ListRemoteAreasAsync(id, ct), ct);
        }
        catch (InvalidOperationException ex)
        {
            ThrowError(ex.Message, StatusCodes.Status404NotFound);
        }
    }
}

public sealed class CreateAdminRemoteAreaEndpoint : Endpoint<UpsertAdminRemoteAreaRequest, AdminRemoteAreaDto>
{
    private readonly IAdminDeliveryService _delivery;

    public CreateAdminRemoteAreaEndpoint(IAdminDeliveryService delivery) => _delivery = delivery;

    public override void Configure()
    {
        Post("admin/cities/{cityId}/remote-areas");
        Policies(AuthPolicies.Admin);
    }

    public override async Task HandleAsync(UpsertAdminRemoteAreaRequest req, CancellationToken ct)
    {
        if (!int.TryParse(Route<string>("cityId"), out var id) || id <= 0)
        {
            ThrowError("Invalid city id.", StatusCodes.Status400BadRequest);
            return;
        }

        try
        {
            var created = await _delivery.CreateRemoteAreaAsync(id, req, ct);
            HttpContext.Response.StatusCode = StatusCodes.Status201Created;
            await Send.OkAsync(created, ct);
        }
        catch (InvalidOperationException ex)
        {
            ThrowError(ex.Message, StatusCodes.Status400BadRequest);
        }
    }
}

public sealed class UpdateAdminRemoteAreaEndpoint : Endpoint<UpsertAdminRemoteAreaRequest, AdminRemoteAreaDto>
{
    private readonly IAdminDeliveryService _delivery;

    public UpdateAdminRemoteAreaEndpoint(IAdminDeliveryService delivery) => _delivery = delivery;

    public override void Configure()
    {
        Put("admin/remote-areas/{remoteAreaId}");
        Policies(AuthPolicies.Admin);
    }

    public override async Task HandleAsync(UpsertAdminRemoteAreaRequest req, CancellationToken ct)
    {
        if (!int.TryParse(Route<string>("remoteAreaId"), out var id) || id <= 0)
        {
            ThrowError("Invalid remote area id.", StatusCodes.Status400BadRequest);
            return;
        }

        try
        {
            await Send.OkAsync(await _delivery.UpdateRemoteAreaAsync(id, req, ct), ct);
        }
        catch (InvalidOperationException ex)
        {
            ThrowError(ex.Message, StatusCodes.Status400BadRequest);
        }
    }
}

public sealed class DeleteAdminRemoteAreaEndpoint : EndpointWithoutRequest
{
    private readonly IAdminDeliveryService _delivery;

    public DeleteAdminRemoteAreaEndpoint(IAdminDeliveryService delivery) => _delivery = delivery;

    public override void Configure()
    {
        Delete("admin/remote-areas/{remoteAreaId}");
        Policies(AuthPolicies.Admin);
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        if (!int.TryParse(Route<string>("remoteAreaId"), out var id) || id <= 0)
        {
            ThrowError("Invalid remote area id.", StatusCodes.Status400BadRequest);
            return;
        }

        try
        {
            await _delivery.DeleteRemoteAreaAsync(id, ct);
            await Send.NoContentAsync(ct);
        }
        catch (InvalidOperationException ex)
        {
            ThrowError(ex.Message, StatusCodes.Status404NotFound);
        }
    }
}
