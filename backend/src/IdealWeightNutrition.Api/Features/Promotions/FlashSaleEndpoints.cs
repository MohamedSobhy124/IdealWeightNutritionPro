using FastEndpoints;
using IdealWeightNutrition.Application.Abstractions;
using IdealWeightNutrition.Contracts.Promotions;

namespace IdealWeightNutrition.Api.Features.Promotions;

public sealed class ListFlashSalesEndpoint : EndpointWithoutRequest<IReadOnlyList<FlashSaleSummaryDto>>
{
    private readonly IFlashSaleService _flashSales;

    public ListFlashSalesEndpoint(IFlashSaleService flashSales) => _flashSales = flashSales;

    public override void Configure()
    {
        Get("flash-sales");
        AllowAnonymous();
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var sales = await _flashSales.ListActiveAsync(ct);
        await Send.OkAsync(sales, ct);
    }
}

public sealed class GetFlashSaleEndpoint : EndpointWithoutRequest<FlashSaleDetailDto>
{
    private readonly IFlashSaleService _flashSales;

    public GetFlashSaleEndpoint(IFlashSaleService flashSales) => _flashSales = flashSales;

    public override void Configure()
    {
        Get("flash-sales/{id}");
        AllowAnonymous();
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        if (!int.TryParse(Route<string>("id"), out var id) || id <= 0)
        {
            ThrowError("Invalid flash sale id.", StatusCodes.Status400BadRequest);
            return;
        }

        var sale = await _flashSales.GetActiveAsync(id, ct);
        if (sale is null)
            ThrowError("Flash sale not found or not active.", StatusCodes.Status404NotFound);
        else
            await Send.OkAsync(sale, ct);
    }
}

public sealed class ListFlashSaleProductPricesEndpoint : EndpointWithoutRequest<IReadOnlyList<FlashSaleProductPriceDto>>
{
    private readonly IFlashSaleService _flashSales;

    public ListFlashSaleProductPricesEndpoint(IFlashSaleService flashSales) => _flashSales = flashSales;

    public override void Configure()
    {
        Get("flash-sales/product-prices");
        AllowAnonymous();
        Summary(s => s.Summary = "Active flash-sale prices keyed by product (and optional variant).");
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var prices = await _flashSales.ListActiveProductPricesAsync(ct);
        await Send.OkAsync(prices, ct);
    }
}
