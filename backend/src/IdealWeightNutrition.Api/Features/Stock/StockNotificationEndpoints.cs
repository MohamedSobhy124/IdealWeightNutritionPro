using FastEndpoints;
using IdealWeightNutrition.Api.Http;
using IdealWeightNutrition.Application.Abstractions;
using IdealWeightNutrition.Contracts.Stock;

namespace IdealWeightNutrition.Api.Features.Stock;

public sealed class SubscribeStockNotificationEndpoint : Endpoint<StockNotificationSubscribeRequest, StockNotificationSubscribeResponse>
{
    private readonly IStockNotificationService _stockNotifications;

    public SubscribeStockNotificationEndpoint(IStockNotificationService stockNotifications) =>
        _stockNotifications = stockNotifications;

    public override void Configure()
    {
        Post("products/{productId}/stock-notify");
        AllowAnonymous();
        Options(o => o.RequireRateLimiting(RateLimitPolicies.PublicForms));
    }

    public override async Task HandleAsync(StockNotificationSubscribeRequest req, CancellationToken ct)
    {
        if (!int.TryParse(Route<string>("productId"), out var productId) || productId <= 0)
        {
            ThrowError("Invalid product id.", StatusCodes.Status400BadRequest);
            return;
        }

        var userId = CartHttp.GetUserId(User);
        var email = req.Email;
        if (string.IsNullOrWhiteSpace(email) && User.Identity?.IsAuthenticated == true)
            email = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;

        try
        {
            var result = await _stockNotifications.SubscribeAsync(
                productId,
                email,
                req.PhoneNumber,
                req.ProductVariantId,
                userId,
                ct);
            await Send.OkAsync(result, ct);
        }
        catch (InvalidOperationException ex)
        {
            ThrowError(ex.Message, StatusCodes.Status400BadRequest);
        }
    }
}
