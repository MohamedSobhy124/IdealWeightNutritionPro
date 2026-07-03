using FastEndpoints;
using IdealWeightNutrition.Api.Http;
using IdealWeightNutrition.Application.Abstractions;
using IdealWeightNutrition.Contracts.Services;
using AuthPolicies = IdealWeightNutrition.Domain.Constants.Policies;

namespace IdealWeightNutrition.Api.Features.Services;

public sealed class ListMyServicePurchasesEndpoint : EndpointWithoutRequest<IReadOnlyList<ServicePurchaseSummaryDto>>
{
    private readonly IServicePurchaseService _purchases;

    public ListMyServicePurchasesEndpoint(IServicePurchaseService purchases) => _purchases = purchases;

    public override void Configure()
    {
        Get("services/my-purchases");
        Policies(AuthPolicies.Customer);
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var userId = CartHttp.GetUserId(User);
        if (string.IsNullOrEmpty(userId))
        {
            await Send.UnauthorizedAsync(ct);
            return;
        }

        var items = await _purchases.ListForUserAsync(userId, ct);
        await Send.OkAsync(items, ct);
    }
}

public sealed class GetMyServicePurchaseEndpoint : EndpointWithoutRequest<ServicePurchaseDetailDto>
{
    private readonly IServicePurchaseService _purchases;

    public GetMyServicePurchaseEndpoint(IServicePurchaseService purchases) => _purchases = purchases;

    public override void Configure()
    {
        Get("services/purchases/{purchaseId}");
        Policies(AuthPolicies.Customer);
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        if (!int.TryParse(Route<string>("purchaseId"), out var purchaseId) || purchaseId <= 0)
        {
            ThrowError("Invalid purchase id.", StatusCodes.Status400BadRequest);
            return;
        }

        var userId = CartHttp.GetUserId(User);
        if (string.IsNullOrEmpty(userId))
        {
            await Send.UnauthorizedAsync(ct);
            return;
        }

        var purchase = await _purchases.GetForUserAsync(purchaseId, userId, ct);
        if (purchase is null)
            ThrowError("Purchase not found.", StatusCodes.Status404NotFound);
        else
            await Send.OkAsync(purchase, ct);
    }
}
