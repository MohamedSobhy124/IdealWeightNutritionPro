using FastEndpoints;
using IdealWeightNutrition.Application.Abstractions;
using IdealWeightNutrition.Contracts.Promotions;

namespace IdealWeightNutrition.Api.Features.Promotions;

public sealed class ListComboOffersEndpoint : EndpointWithoutRequest<IReadOnlyList<ComboOfferSummaryDto>>
{
    private readonly IComboOfferService _combos;

    public ListComboOffersEndpoint(IComboOfferService combos) => _combos = combos;

    public override void Configure()
    {
        Get("combo-offers");
        AllowAnonymous();
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var offers = await _combos.ListActiveAsync(ct);
        await Send.OkAsync(offers, ct);
    }
}

public sealed class GetComboOfferEndpoint : EndpointWithoutRequest<ComboOfferDetailDto>
{
    private readonly IComboOfferService _combos;

    public GetComboOfferEndpoint(IComboOfferService combos) => _combos = combos;

    public override void Configure()
    {
        Get("combo-offers/{id}");
        AllowAnonymous();
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        if (!int.TryParse(Route<string>("id"), out var id) || id <= 0)
        {
            ThrowError("Invalid combo offer id.", StatusCodes.Status400BadRequest);
            return;
        }

        var offer = await _combos.GetActiveAsync(id, ct);
        if (offer is null)
            ThrowError("Combo offer not found or not active.", StatusCodes.Status404NotFound);
        else
            await Send.OkAsync(offer, ct);
    }
}
