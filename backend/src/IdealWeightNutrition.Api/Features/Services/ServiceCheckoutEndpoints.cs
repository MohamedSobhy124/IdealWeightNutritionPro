using FastEndpoints;
using IdealWeightNutrition.Api.Http;
using IdealWeightNutrition.Application.Abstractions;
using IdealWeightNutrition.Contracts.Checkout;
using IdealWeightNutrition.Contracts.Services;

namespace IdealWeightNutrition.Api.Features.Services;

public sealed class ServiceCheckoutQuoteEndpoint : Endpoint<ServiceCheckoutQuoteRequest, ServiceCheckoutQuoteResponse>
{
    private readonly IServiceCheckoutService _checkout;

    public ServiceCheckoutQuoteEndpoint(IServiceCheckoutService checkout) => _checkout = checkout;

    public override void Configure()
    {
        Post("services/checkout/quote");
        AllowAnonymous();
    }

    public override async Task HandleAsync(ServiceCheckoutQuoteRequest req, CancellationToken ct)
    {
        try
        {
            var quote = await _checkout.GetQuoteAsync(req, ct);
            await Send.OkAsync(quote, ct);
        }
        catch (InvalidOperationException ex)
        {
            ThrowError(ex.Message, StatusCodes.Status400BadRequest);
        }
    }
}

public sealed class ServiceCheckoutOtpEndpoint : Endpoint<CheckoutOtpRequest>
{
    private readonly IServiceCheckoutService _checkout;

    public ServiceCheckoutOtpEndpoint(IServiceCheckoutService checkout) => _checkout = checkout;

    public override void Configure()
    {
        Post("services/checkout/otp");
        AllowAnonymous();
        Options(o => o.RequireRateLimiting(RateLimitPolicies.Auth));
    }

    public override async Task HandleAsync(CheckoutOtpRequest req, CancellationToken ct)
    {
        try
        {
            await _checkout.RequestCheckoutOtpAsync(req.Email, ct);
            await Send.OkAsync(new { message = "Verification code sent." }, ct);
        }
        catch (InvalidOperationException ex)
        {
            ThrowError(ex.Message, StatusCodes.Status400BadRequest);
        }
    }
}

public sealed class ServiceVerifyCheckoutOtpEndpoint : Endpoint<VerifyCheckoutOtpRequest, OtpVerificationResult>
{
    private readonly IServiceCheckoutService _checkout;

    public ServiceVerifyCheckoutOtpEndpoint(IServiceCheckoutService checkout) => _checkout = checkout;

    public override void Configure()
    {
        Post("services/checkout/verify-otp");
        AllowAnonymous();
        Options(o => o.RequireRateLimiting(RateLimitPolicies.Auth));
    }

    public override async Task HandleAsync(VerifyCheckoutOtpRequest req, CancellationToken ct)
    {
        var result = await _checkout.VerifyCheckoutOtpAsync(req.Email, req.Otp, ct);
        await Send.OkAsync(result, ct);
    }
}

public sealed class ServicePaymentMethodsEndpoint : EndpointWithoutRequest<PaymentMethodsResponse>
{
    private readonly IServiceCheckoutService _checkout;

    public ServicePaymentMethodsEndpoint(IServiceCheckoutService checkout) => _checkout = checkout;

    public override void Configure()
    {
        Get("services/checkout/payment-methods");
        AllowAnonymous();
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        if (!double.TryParse(Query<string>("total"), out var total) || total < 0)
        {
            ThrowError("Query parameter 'total' is required.", StatusCodes.Status400BadRequest);
            return;
        }

        var methods = await _checkout.GetPaymentMethodsAsync(total, ct);
        await Send.OkAsync(methods, ct);
    }
}

public sealed class CreateServicePurchaseEndpoint : Endpoint<CreateServicePurchaseRequest, CreateServicePurchaseResponse>
{
    private readonly IServiceCheckoutService _checkout;

    public CreateServicePurchaseEndpoint(IServiceCheckoutService checkout) => _checkout = checkout;

    public override void Configure()
    {
        Post("services/{id}/purchase");
        AllowAnonymous();
    }

    public override async Task HandleAsync(CreateServicePurchaseRequest req, CancellationToken ct)
    {
        if (!int.TryParse(Route<string>("id"), out var serviceId) || serviceId <= 0)
        {
            ThrowError("Invalid service id.", StatusCodes.Status400BadRequest);
            return;
        }

        try
        {
            var result = await _checkout.CreatePurchaseAsync(
                serviceId,
                CartHttp.GetUserId(User),
                req,
                ct);
            await Send.OkAsync(result, ct);
        }
        catch (InvalidOperationException ex)
        {
            ThrowError(ex.Message, StatusCodes.Status400BadRequest);
        }
    }
}

public sealed class CompleteServicePaymentEndpoint : EndpointWithoutRequest<CompleteServicePaymentResponse>
{
    private readonly IServiceCheckoutService _checkout;

    public CompleteServicePaymentEndpoint(IServiceCheckoutService checkout) => _checkout = checkout;

    public override void Configure()
    {
        Post("services/purchases/{purchaseId}/complete-payment");
        AllowAnonymous();
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        if (!int.TryParse(Route<string>("purchaseId"), out var purchaseId))
        {
            ThrowError("Invalid purchase id.", StatusCodes.Status400BadRequest);
            return;
        }

        try
        {
            var result = await _checkout.CompletePaymentAsync(purchaseId, ct);
            await Send.OkAsync(result, ct);
        }
        catch (InvalidOperationException ex)
        {
            ThrowError(ex.Message, StatusCodes.Status400BadRequest);
        }
    }
}
