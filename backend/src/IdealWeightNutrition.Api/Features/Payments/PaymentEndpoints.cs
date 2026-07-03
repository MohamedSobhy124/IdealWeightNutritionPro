using FastEndpoints;
using IdealWeightNutrition.Api.Http;
using IdealWeightNutrition.Application.Abstractions;
using IdealWeightNutrition.Contracts.Checkout;
using IdealWeightNutrition.Infrastructure.Options;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace IdealWeightNutrition.Api.Features.Payments;

public sealed class GeideaCallbackEndpoint : EndpointWithoutRequest
{
    private readonly IPaymentService _payments;

    public GeideaCallbackEndpoint(IPaymentService payments) => _payments = payments;

    public override void Configure()
    {
        Post("payments/geidea/callback");
        AllowAnonymous();
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        if (!int.TryParse(Query<string>("orderId"), out var orderId))
        {
            await Send.OkAsync(new { status = "error", message = "Invalid order id." }, ct);
            return;
        }

        await _payments.HandleGeideaCallbackAsync(orderId, ct);
        await Send.OkAsync(new { status = "success" }, ct);
    }
}

public sealed class GeideaServiceCallbackEndpoint : EndpointWithoutRequest
{
    private readonly IPaymentService _payments;

    public GeideaServiceCallbackEndpoint(IPaymentService payments) => _payments = payments;

    public override void Configure()
    {
        Post("payments/geidea/service-callback");
        AllowAnonymous();
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        if (!int.TryParse(Query<string>("purchaseId"), out var purchaseId))
        {
            await Send.OkAsync(new { status = "error", message = "Invalid purchase id." }, ct);
            return;
        }

        await _payments.HandleServiceGeideaCallbackAsync(purchaseId, ct);
        await Send.OkAsync(new { status = "success" }, ct);
    }
}

public sealed class TamaraWebhookEndpoint : EndpointWithoutRequest<TamaraWebhookAckResponse>
{
    private readonly IPaymentService _payments;
    private readonly ILogger<TamaraWebhookEndpoint> _logger;

    public TamaraWebhookEndpoint(IPaymentService payments, ILogger<TamaraWebhookEndpoint> logger)
    {
        _payments = payments;
        _logger = logger;
    }

    public override void Configure()
    {
        Post("payments/tamara/webhook");
        AllowAnonymous();
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        using var reader = new StreamReader(HttpContext.Request.Body);
        var body = await reader.ReadToEndAsync(ct);

        _logger.LogInformation("Tamara webhook received ({Length} bytes).", body.Length);

        TamaraNotificationPayload? notification;
        try
        {
            notification = JsonSerializer.Deserialize<TamaraNotificationPayload>(body);
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Failed to parse Tamara webhook payload.");
            await Send.OkAsync(new TamaraWebhookAckResponse
            {
                Success = false,
                Message = "Invalid JSON payload."
            }, ct);
            return;
        }

        if (notification is null)
        {
            await Send.OkAsync(new TamaraWebhookAckResponse
            {
                Success = false,
                Message = "Invalid payload."
            }, ct);
            return;
        }

        var authHeader = HttpContext.Request.Headers.Authorization.ToString();
        var result = await _payments.HandleTamaraWebhookAsync(notification, authHeader, ct);
        await Send.OkAsync(result, ct);
    }
}

public sealed class TamaraReturnEndpoint : EndpointWithoutRequest
{
    private readonly IPaymentService _payments;
    private readonly AppUrlOptions _urls;

    public TamaraReturnEndpoint(IPaymentService payments, IOptions<AppUrlOptions> urls)
    {
        _payments = payments;
        _urls = urls.Value;
    }

    public override void Configure()
    {
        Get("payments/tamara/return");
        AllowAnonymous();
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        if (!int.TryParse(Query<string>("orderId"), out var orderId))
        {
            HttpContext.Response.Redirect($"{_urls.FrontendBaseUrl.TrimEnd('/')}/checkout");
            return;
        }

        var status = Query<string>("status") ?? string.Empty;
        var tamaraOrderId = Query<string>("tamara_order_id") ?? Query<string>("order_id");

        await _payments.HandleTamaraReturnAsync(
            orderId,
            status,
            tamaraOrderId,
            CartHttp.GetUserId(User),
            CartHttp.GetGuestCartId(HttpContext),
            ct);

        HttpContext.Response.Redirect($"{_urls.FrontendBaseUrl.TrimEnd('/')}/order/confirmation/{orderId}");
    }
}

public sealed class TamaraServiceReturnEndpoint : EndpointWithoutRequest
{
    private readonly IPaymentService _payments;
    private readonly AppUrlOptions _urls;

    public TamaraServiceReturnEndpoint(IPaymentService payments, IOptions<AppUrlOptions> urls)
    {
        _payments = payments;
        _urls = urls.Value;
    }

    public override void Configure()
    {
        Get("payments/tamara/service-return");
        AllowAnonymous();
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        if (!int.TryParse(Query<string>("purchaseId"), out var purchaseId))
        {
            HttpContext.Response.Redirect($"{_urls.FrontendBaseUrl.TrimEnd('/')}/services");
            return;
        }

        var status = Query<string>("status") ?? string.Empty;
        var tamaraOrderId = Query<string>("tamara_order_id") ?? Query<string>("order_id");

        await _payments.HandleServiceTamaraReturnAsync(purchaseId, status, tamaraOrderId, ct);

        HttpContext.Response.Redirect(
            $"{_urls.FrontendBaseUrl.TrimEnd('/')}/services/confirmation/{purchaseId}");
    }
}

public sealed class TabbyReturnEndpoint : EndpointWithoutRequest
{
    private readonly IPaymentService _payments;
    private readonly AppUrlOptions _urls;

    public TabbyReturnEndpoint(IPaymentService payments, IOptions<AppUrlOptions> urls)
    {
        _payments = payments;
        _urls = urls.Value;
    }

    public override void Configure()
    {
        Get("payments/tabby/return");
        AllowAnonymous();
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        if (!int.TryParse(Query<string>("orderId"), out var orderId))
        {
            HttpContext.Response.Redirect($"{_urls.FrontendBaseUrl.TrimEnd('/')}/checkout");
            return;
        }

        var status = Query<string>("status") ?? string.Empty;
        var paymentId = Query<string>("payment_id");

        await _payments.HandleTabbyReturnAsync(
            orderId,
            status,
            paymentId,
            CartHttp.GetUserId(User),
            CartHttp.GetGuestCartId(HttpContext),
            ct);

        HttpContext.Response.Redirect($"{_urls.FrontendBaseUrl.TrimEnd('/')}/order/confirmation/{orderId}");
    }
}

public sealed class TabbyServiceReturnEndpoint : EndpointWithoutRequest
{
    private readonly IPaymentService _payments;
    private readonly AppUrlOptions _urls;

    public TabbyServiceReturnEndpoint(IPaymentService payments, IOptions<AppUrlOptions> urls)
    {
        _payments = payments;
        _urls = urls.Value;
    }

    public override void Configure()
    {
        Get("payments/tabby/service-return");
        AllowAnonymous();
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        if (!int.TryParse(Query<string>("purchaseId"), out var purchaseId))
        {
            HttpContext.Response.Redirect($"{_urls.FrontendBaseUrl.TrimEnd('/')}/services");
            return;
        }

        var status = Query<string>("status") ?? string.Empty;
        var paymentId = Query<string>("payment_id");

        await _payments.HandleServiceTabbyReturnAsync(purchaseId, status, paymentId, ct);

        HttpContext.Response.Redirect(
            $"{_urls.FrontendBaseUrl.TrimEnd('/')}/services/confirmation/{purchaseId}");
    }
}

public sealed class CompletePaymentEndpoint : EndpointWithoutRequest<CompletePaymentResponse>
{
    private readonly ICheckoutService _checkout;

    public CompletePaymentEndpoint(ICheckoutService checkout) => _checkout = checkout;

    public override void Configure()
    {
        Post("checkout/orders/{orderId}/complete-payment");
        AllowAnonymous();
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        if (!int.TryParse(Route<string>("orderId"), out var orderId))
        {
            ThrowError("Invalid order.", StatusCodes.Status400BadRequest);
            return;
        }

        try
        {
            var result = await _checkout.CompletePaymentAsync(
                orderId,
                CartHttp.GetUserId(User),
                CartHttp.GetGuestCartId(HttpContext),
                ct);
            await Send.OkAsync(result, ct);
        }
        catch (InvalidOperationException ex)
        {
            ThrowError(ex.Message, StatusCodes.Status400BadRequest);
        }
    }
}

public sealed class GetPaymentMethodsEndpoint : EndpointWithoutRequest<PaymentMethodsResponse>
{
    private readonly ICheckoutService _checkout;

    public GetPaymentMethodsEndpoint(ICheckoutService checkout) => _checkout = checkout;

    public override void Configure()
    {
        Get("checkout/payment-methods");
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
