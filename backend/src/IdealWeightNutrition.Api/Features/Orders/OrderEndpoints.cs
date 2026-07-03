using FastEndpoints;
using IdealWeightNutrition.Api.Http;
using IdealWeightNutrition.Application.Abstractions;
using IdealWeightNutrition.Contracts.Orders;
using AuthPolicies = IdealWeightNutrition.Domain.Constants.Policies;

namespace IdealWeightNutrition.Api.Features.Orders;

public sealed class ListMyOrdersEndpoint : EndpointWithoutRequest<IReadOnlyList<OrderSummaryDto>>
{
    private readonly IOrderService _orders;

    public ListMyOrdersEndpoint(IOrderService orders) => _orders = orders;

    public override void Configure()
    {
        Get("orders");
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

        var orders = await _orders.ListUserOrdersAsync(userId, ct);
        await Send.OkAsync(orders, ct);
    }
}

public sealed class GetOrderEndpoint : EndpointWithoutRequest<OrderDto>
{
    private readonly IOrderService _orders;

    public GetOrderEndpoint(IOrderService orders) => _orders = orders;

    public override void Configure()
    {
        Get("orders/{orderId}");
        AllowAnonymous();
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        if (!int.TryParse(Route<string>("orderId"), out var orderId) || orderId <= 0)
        {
            ThrowError("Invalid order id.", StatusCodes.Status400BadRequest);
            return;
        }

        var guestEmail = Query<string>("email");
        var order = await _orders.GetOrderAsync(
            orderId,
            CartHttp.GetUserId(User),
            guestEmail,
            ct);

        if (order is null)
            ThrowError("Order not found.", StatusCodes.Status404NotFound);
        else
            await Send.OkAsync(order, ct);
    }
}

public sealed class TrackOrderEndpoint : Endpoint<TrackOrderRequest, OrderDto>
{
    private readonly IOrderService _orders;

    public TrackOrderEndpoint(IOrderService orders) => _orders = orders;

    public override void Configure()
    {
        Post("orders/track");
        AllowAnonymous();
    }

    public override async Task HandleAsync(TrackOrderRequest req, CancellationToken ct)
    {
        if (req.OrderId <= 0 || string.IsNullOrWhiteSpace(req.Email))
        {
            ThrowError("Order id and email are required.", StatusCodes.Status400BadRequest);
            return;
        }

        var order = await _orders.TrackOrderAsync(req, CartHttp.GetUserId(User), ct);
        if (order is null)
            ThrowError("Order not found. Check your order id and email.", StatusCodes.Status404NotFound);
        else
            await Send.OkAsync(order, ct);
    }
}

public sealed class GetOrderInvoiceEndpoint : EndpointWithoutRequest
{
    private readonly IOrderNotificationService _notifications;

    public GetOrderInvoiceEndpoint(IOrderNotificationService notifications) => _notifications = notifications;

    public override void Configure()
    {
        Get("orders/{orderId}/invoice");
        AllowAnonymous();
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        if (!int.TryParse(Route<string>("orderId"), out var orderId) || orderId <= 0)
        {
            ThrowError("Invalid order id.", StatusCodes.Status400BadRequest);
            return;
        }

        var guestEmail = Query<string>("email");
        var pdf = await _notifications.GenerateInvoicePdfAsync(
            orderId,
            CartHttp.GetUserId(User),
            guestEmail,
            ct);

        if (pdf is null or { Length: 0 })
            ThrowError("Invoice not found.", StatusCodes.Status404NotFound);
        else
            await Send.BytesAsync(pdf, $"Invoice-{orderId}.pdf", "application/pdf", cancellation: ct);
    }
}
