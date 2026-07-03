using FastEndpoints;
using IdealWeightNutrition.Api.Http;
using IdealWeightNutrition.Application.Abstractions;
using IdealWeightNutrition.Contracts.Checkout;
using Microsoft.Extensions.Logging;

namespace IdealWeightNutrition.Api.Features.Checkout;

public sealed class ListCitiesEndpoint : EndpointWithoutRequest<IReadOnlyList<CityDto>>
{
    private readonly ICheckoutService _checkout;

    public ListCitiesEndpoint(ICheckoutService checkout) => _checkout = checkout;

    public override void Configure()
    {
        Get("checkout/cities");
        AllowAnonymous();
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var cities = await _checkout.ListCitiesAsync(ct);
        await Send.OkAsync(cities, ct);
    }
}

public sealed class ListRemoteAreasEndpoint : EndpointWithoutRequest<IReadOnlyList<RemoteAreaDto>>
{
    private readonly ICheckoutService _checkout;

    public ListRemoteAreasEndpoint(ICheckoutService checkout) => _checkout = checkout;

    public override void Configure()
    {
        Get("checkout/cities/{cityId}/remote-areas");
        AllowAnonymous();
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        if (!int.TryParse(Route<string>("cityId"), out var cityId))
        {
            ThrowError("Invalid city.", StatusCodes.Status400BadRequest);
            return;
        }

        var areas = await _checkout.ListRemoteAreasAsync(cityId, ct);
        await Send.OkAsync(areas, ct);
    }
}

public sealed class ShippingQuoteEndpoint : Endpoint<ShippingQuoteRequest, ShippingQuoteResponse>
{
    private readonly ICheckoutService _checkout;

    public ShippingQuoteEndpoint(ICheckoutService checkout) => _checkout = checkout;

    public override void Configure()
    {
        Post("checkout/shipping-quote");
        AllowAnonymous();
    }

    public override async Task HandleAsync(ShippingQuoteRequest req, CancellationToken ct)
    {
        try
        {
            var quote = await _checkout.GetShippingQuoteAsync(
                CartHttp.GetUserId(User),
                CartHttp.GetGuestCartId(HttpContext),
                req,
                ct);
            await Send.OkAsync(quote, ct);
        }
        catch (InvalidOperationException ex)
        {
            ThrowError(ex.Message, StatusCodes.Status400BadRequest);
        }
    }
}

public sealed class RequestCheckoutOtpEndpoint : Endpoint<CheckoutOtpRequest, OtpActionResponse>
{
    private readonly ICheckoutService _checkout;

    public RequestCheckoutOtpEndpoint(ICheckoutService checkout) => _checkout = checkout;

    public override void Configure()
    {
        Post("checkout/otp");
        AllowAnonymous();
        Options(o => o.RequireRateLimiting(RateLimitPolicies.Auth));
    }

    public override async Task HandleAsync(CheckoutOtpRequest req, CancellationToken ct)
    {
        try
        {
            await _checkout.RequestCheckoutOtpAsync(req.Email, ct);
            await Send.OkAsync(new OtpActionResponse { Message = "Verification code sent to your email." }, ct);
        }
        catch (InvalidOperationException ex)
        {
            ThrowError(ex.Message, StatusCodes.Status400BadRequest);
        }
    }
}

public sealed class VerifyCheckoutOtpEndpoint : Endpoint<VerifyCheckoutOtpRequest, OtpActionResponse>
{
    private readonly ICheckoutService _checkout;

    public VerifyCheckoutOtpEndpoint(ICheckoutService checkout) => _checkout = checkout;

    public override void Configure()
    {
        Post("checkout/verify-otp");
        AllowAnonymous();
        Options(o => o.RequireRateLimiting(RateLimitPolicies.Auth));
    }

    public override async Task HandleAsync(VerifyCheckoutOtpRequest req, CancellationToken ct)
    {
        var result = await _checkout.VerifyCheckoutOtpAsync(req.Email, req.Otp, ct);
        if (!result.IsValid)
        {
            ThrowError(result.Message ?? "Invalid code.", StatusCodes.Status400BadRequest);
            return;
        }

        await Send.OkAsync(new OtpActionResponse { Message = result.Message ?? "Email verified." }, ct);
    }
}

public sealed class CreateOrderEndpoint : Endpoint<CreateOrderRequest, CreateOrderResponse>
{
    private readonly ICheckoutService _checkout;
    private readonly ILogger<CreateOrderEndpoint> _logger;

    public CreateOrderEndpoint(ICheckoutService checkout, ILogger<CreateOrderEndpoint> logger)
    {
        _checkout = checkout;
        _logger = logger;
    }

    public override void Configure()
    {
        Post("checkout");
        AllowAnonymous();
    }

    public override async Task HandleAsync(CreateOrderRequest req, CancellationToken ct)
    {
        try
        {
            var order = await _checkout.CreateOrderAsync(
                CartHttp.GetUserId(User),
                CartHttp.GetGuestCartId(HttpContext),
                req,
                ct);
            await Send.OkAsync(order, ct);
        }
        catch (InvalidOperationException ex)
        {
            ThrowError(ex.Message, StatusCodes.Status400BadRequest);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Checkout failed");
            ThrowError(ex.Message, StatusCodes.Status500InternalServerError);
        }
    }
}

public sealed class OtpActionResponse
{
    public required string Message { get; init; }
}
