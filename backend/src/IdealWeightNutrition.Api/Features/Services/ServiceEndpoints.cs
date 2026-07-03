using FastEndpoints;
using IdealWeightNutrition.Application.Abstractions;
using IdealWeightNutrition.Contracts.Services;

namespace IdealWeightNutrition.Api.Features.Services;

public sealed class ListServiceSubscriptionsEndpoint : EndpointWithoutRequest<IReadOnlyList<ServiceSubscriptionSummaryDto>>
{
    private readonly IServiceSubscriptionService _services;

    public ListServiceSubscriptionsEndpoint(IServiceSubscriptionService services) => _services = services;

    public override void Configure()
    {
        Get("services");
        AllowAnonymous();
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var items = await _services.ListActiveAsync(ct);
        await Send.OkAsync(items, ct);
    }
}

public sealed class GetServiceSubscriptionEndpoint : EndpointWithoutRequest<ServiceSubscriptionDetailDto>
{
    private readonly IServiceSubscriptionService _services;

    public GetServiceSubscriptionEndpoint(IServiceSubscriptionService services) => _services = services;

    public override void Configure()
    {
        Get("services/{id}");
        AllowAnonymous();
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        if (!int.TryParse(Route<string>("id"), out var id) || id <= 0)
        {
            ThrowError("Invalid service id.", StatusCodes.Status400BadRequest);
            return;
        }

        var service = await _services.GetActiveAsync(id, ct);
        if (service is null)
            ThrowError("Service not found or not active.", StatusCodes.Status404NotFound);
        else
            await Send.OkAsync(service, ct);
    }
}
