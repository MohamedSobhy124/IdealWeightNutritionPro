using FastEndpoints;
using IdealWeightNutrition.Application.Abstractions;
using IdealWeightNutrition.Contracts.Orders;
using AuthPolicies = IdealWeightNutrition.Domain.Constants.Policies;

namespace IdealWeightNutrition.Api.Features.Admin;

public sealed class ListAdminOrdersRequest
{
    public string? Status { get; init; }
    public string? PaymentStatus { get; init; }
    public string? PaymentMethod { get; init; }
    public DateTime? DateFrom { get; init; }
    public DateTime? DateTo { get; init; }
    public string? Search { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 50;
}

public sealed class ListAdminOrdersEndpoint : Endpoint<ListAdminOrdersRequest, AdminOrderListResponse>
{
    private readonly IAdminOrderService _orders;

    public ListAdminOrdersEndpoint(IAdminOrderService orders) => _orders = orders;

    public override void Configure()
    {
        Get("admin/orders");
        Policies(AuthPolicies.Employee);
    }

    public override async Task HandleAsync(ListAdminOrdersRequest req, CancellationToken ct) =>
        await Send.OkAsync(await _orders.ListOrdersAsync(MapQuery(req), ct), ct);

    private static AdminOrderQuery MapQuery(ListAdminOrdersRequest req) => new()
    {
        Status = req.Status,
        PaymentStatus = req.PaymentStatus,
        PaymentMethod = req.PaymentMethod,
        DateFrom = req.DateFrom,
        DateTo = req.DateTo,
        Search = req.Search,
        Page = req.Page,
        PageSize = req.PageSize
    };
}

public sealed class ExportAdminOrdersEndpoint : Endpoint<ListAdminOrdersRequest>
{
    private readonly IAdminOrderService _orders;
    private readonly IDateTimeProvider _clock;

    public ExportAdminOrdersEndpoint(IAdminOrderService orders, IDateTimeProvider clock)
    {
        _orders = orders;
        _clock = clock;
    }

    public override void Configure()
    {
        Get("admin/orders/export");
        Policies(AuthPolicies.Employee);
    }

    public override async Task HandleAsync(ListAdminOrdersRequest req, CancellationToken ct)
    {
        var bytes = await _orders.ExportCsvAsync(MapQuery(req), ct);
        var fileName = $"Orders_Export_{_clock.Now:yyyyMMdd_HHmmss}.csv";
        HttpContext.Response.ContentType = "text/csv";
        HttpContext.Response.Headers.ContentDisposition = $"attachment; filename=\"{fileName}\"";
        await HttpContext.Response.Body.WriteAsync(bytes, ct);
    }

    private static AdminOrderQuery MapQuery(ListAdminOrdersRequest req) => new()
    {
        Status = req.Status,
        PaymentStatus = req.PaymentStatus,
        PaymentMethod = req.PaymentMethod,
        DateFrom = req.DateFrom,
        DateTo = req.DateTo,
        Search = req.Search,
        Page = 1,
        PageSize = int.MaxValue
    };
}

public sealed class ExportProductProfitsRequest
{
    public DateTime? DateFrom { get; init; }
    public DateTime? DateTo { get; init; }
}

public sealed class ExportProductProfitsEndpoint : Endpoint<ExportProductProfitsRequest>
{
    private readonly IAdminOrderService _orders;
    private readonly IDateTimeProvider _clock;

    public ExportProductProfitsEndpoint(IAdminOrderService orders, IDateTimeProvider clock)
    {
        _orders = orders;
        _clock = clock;
    }

    public override void Configure()
    {
        Get("admin/orders/export-product-profits");
        Policies(AuthPolicies.Admin);
    }

    public override async Task HandleAsync(ExportProductProfitsRequest req, CancellationToken ct)
    {
        var bytes = await _orders.ExportProductProfitsCsvAsync(req.DateFrom, req.DateTo, ct);
        var fileName = $"Product_Profits_{_clock.Now:yyyyMMdd_HHmmss}.csv";
        HttpContext.Response.ContentType = "text/csv";
        HttpContext.Response.Headers.ContentDisposition = $"attachment; filename=\"{fileName}\"";
        await HttpContext.Response.Body.WriteAsync(bytes, ct);
    }
}

public sealed class GetAdminOrderStatisticsEndpoint : EndpointWithoutRequest<AdminOrderStatisticsDto>
{
    private readonly IAdminOrderService _orders;

    public GetAdminOrderStatisticsEndpoint(IAdminOrderService orders) => _orders = orders;

    public override void Configure()
    {
        Get("admin/orders/statistics");
        Policies(AuthPolicies.Employee);
    }

    public override async Task HandleAsync(CancellationToken ct) =>
        await Send.OkAsync(await _orders.GetStatisticsAsync(ct), ct);
}

public sealed class GetAdminOrderAuditLogEndpoint : EndpointWithoutRequest<IReadOnlyList<AdminOrderAuditLogDto>>
{
    private readonly IAdminOrderService _orders;

    public GetAdminOrderAuditLogEndpoint(IAdminOrderService orders) => _orders = orders;

    public override void Configure()
    {
        Get("admin/orders/{orderId}/audit-log");
        Policies(AuthPolicies.Employee);
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        if (!int.TryParse(Route<string>("orderId"), out var orderId) || orderId <= 0)
        {
            ThrowError("Invalid order id.", StatusCodes.Status400BadRequest);
            return;
        }

        await Send.OkAsync(await _orders.GetAuditLogsAsync(orderId, ct), ct);
    }
}

public sealed class GetAdminOrderEndpoint : EndpointWithoutRequest<AdminOrderDetailDto>
{
    private readonly IAdminOrderService _orders;

    public GetAdminOrderEndpoint(IAdminOrderService orders) => _orders = orders;

    public override void Configure()
    {
        Get("admin/orders/{orderId}");
        Policies(AuthPolicies.Employee);
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        if (!int.TryParse(Route<string>("orderId"), out var orderId) || orderId <= 0)
        {
            ThrowError("Invalid order id.", StatusCodes.Status400BadRequest);
            return;
        }

        var order = await _orders.GetOrderAsync(orderId, ct);
        if (order is null)
            ThrowError("Order not found.", StatusCodes.Status404NotFound);
        else
            await Send.OkAsync(order, ct);
    }
}

public sealed class StartProcessingOrderEndpoint : EndpointWithoutRequest<AdminOrderActionResponse>
{
    private readonly IAdminOrderService _orders;

    public StartProcessingOrderEndpoint(IAdminOrderService orders) => _orders = orders;

    public override void Configure()
    {
        Post("admin/orders/{orderId}/start-processing");
        Policies(AuthPolicies.Employee);
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var orderId = ParseOrderId();
        try
        {
            var result = await _orders.StartProcessingAsync(orderId, ct);
            await Send.OkAsync(result, ct);
        }
        catch (InvalidOperationException ex)
        {
            ThrowError(ex.Message, StatusCodes.Status400BadRequest);
        }
    }

    private int ParseOrderId()
    {
        if (!int.TryParse(Route<string>("orderId"), out var orderId) || orderId <= 0)
            ThrowError("Invalid order id.", StatusCodes.Status400BadRequest);
        return orderId;
    }
}

public sealed class ShipOrderEndpoint : Endpoint<ShipOrderRequest, AdminOrderActionResponse>
{
    private readonly IAdminOrderService _orders;

    public ShipOrderEndpoint(IAdminOrderService orders) => _orders = orders;

    public override void Configure()
    {
        Post("admin/orders/{orderId}/ship");
        Policies(AuthPolicies.Employee);
    }

    public override async Task HandleAsync(ShipOrderRequest req, CancellationToken ct)
    {
        if (!int.TryParse(Route<string>("orderId"), out var orderId) || orderId <= 0)
        {
            ThrowError("Invalid order id.", StatusCodes.Status400BadRequest);
            return;
        }

        try
        {
            var result = await _orders.ShipOrderAsync(orderId, req, ct);
            await Send.OkAsync(result, ct);
        }
        catch (InvalidOperationException ex)
        {
            ThrowError(ex.Message, StatusCodes.Status400BadRequest);
        }
    }
}

public sealed class MarkOrderDeliveredEndpoint : EndpointWithoutRequest<AdminOrderActionResponse>
{
    private readonly IAdminOrderService _orders;

    public MarkOrderDeliveredEndpoint(IAdminOrderService orders) => _orders = orders;

    public override void Configure()
    {
        Post("admin/orders/{orderId}/deliver");
        Policies(AuthPolicies.Employee);
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        if (!int.TryParse(Route<string>("orderId"), out var orderId) || orderId <= 0)
        {
            ThrowError("Invalid order id.", StatusCodes.Status400BadRequest);
            return;
        }

        try
        {
            var result = await _orders.MarkDeliveredAsync(orderId, ct);
            await Send.OkAsync(result, ct);
        }
        catch (InvalidOperationException ex)
        {
            ThrowError(ex.Message, StatusCodes.Status400BadRequest);
        }
    }
}

public sealed class CancelAdminOrderEndpoint : EndpointWithoutRequest<AdminOrderActionResponse>
{
    private readonly IAdminOrderService _orders;

    public CancelAdminOrderEndpoint(IAdminOrderService orders) => _orders = orders;

    public override void Configure()
    {
        Post("admin/orders/{orderId}/cancel");
        Policies(AuthPolicies.Employee);
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        if (!int.TryParse(Route<string>("orderId"), out var orderId) || orderId <= 0)
        {
            ThrowError("Invalid order id.", StatusCodes.Status400BadRequest);
            return;
        }

        try
        {
            var result = await _orders.CancelOrderAsync(orderId, ct);
            await Send.OkAsync(result, ct);
        }
        catch (InvalidOperationException ex)
        {
            ThrowError(ex.Message, StatusCodes.Status400BadRequest);
        }
    }
}

public sealed class RecheckAdminOrderPaymentEndpoint : EndpointWithoutRequest<AdminOrderActionResponse>
{
    private readonly IAdminOrderService _orders;

    public RecheckAdminOrderPaymentEndpoint(IAdminOrderService orders) => _orders = orders;

    public override void Configure()
    {
        Post("admin/orders/{orderId}/recheck-payment");
        Policies(AuthPolicies.Employee);
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        if (!int.TryParse(Route<string>("orderId"), out var orderId) || orderId <= 0)
        {
            ThrowError("Invalid order id.", StatusCodes.Status400BadRequest);
            return;
        }

        try
        {
            var result = await _orders.RecheckPaymentStatusAsync(orderId, ct);
            await Send.OkAsync(result, ct);
        }
        catch (InvalidOperationException ex)
        {
            ThrowError(ex.Message, StatusCodes.Status400BadRequest);
        }
    }
}

public sealed class ForceCompleteAdminOrderEndpoint : Endpoint<ForceOrderActionRequest, AdminOrderActionResponse>
{
    private readonly IAdminOrderService _orders;

    public ForceCompleteAdminOrderEndpoint(IAdminOrderService orders) => _orders = orders;

    public override void Configure()
    {
        Post("admin/orders/{orderId}/force-complete");
        Policies(AuthPolicies.Employee);
    }

    public override async Task HandleAsync(ForceOrderActionRequest req, CancellationToken ct)
    {
        if (!int.TryParse(Route<string>("orderId"), out var orderId) || orderId <= 0)
        {
            ThrowError("Invalid order id.", StatusCodes.Status400BadRequest);
            return;
        }

        try
        {
            var result = await _orders.ForceCompleteAsync(orderId, req, ct);
            await Send.OkAsync(result, ct);
        }
        catch (InvalidOperationException ex)
        {
            ThrowError(ex.Message, StatusCodes.Status400BadRequest);
        }
    }
}

public sealed class ForceCancelAdminOrderEndpoint : Endpoint<ForceOrderActionRequest, AdminOrderActionResponse>
{
    private readonly IAdminOrderService _orders;

    public ForceCancelAdminOrderEndpoint(IAdminOrderService orders) => _orders = orders;

    public override void Configure()
    {
        Post("admin/orders/{orderId}/force-cancel");
        Policies(AuthPolicies.Employee);
    }

    public override async Task HandleAsync(ForceOrderActionRequest req, CancellationToken ct)
    {
        if (!int.TryParse(Route<string>("orderId"), out var orderId) || orderId <= 0)
        {
            ThrowError("Invalid order id.", StatusCodes.Status400BadRequest);
            return;
        }

        try
        {
            var result = await _orders.ForceCancelAsync(orderId, req, ct);
            await Send.OkAsync(result, ct);
        }
        catch (InvalidOperationException ex)
        {
            ThrowError(ex.Message, StatusCodes.Status400BadRequest);
        }
    }
}

public sealed class UpdateAdminOrderLineEndpoint : Endpoint<UpdateOrderLineRequest, AdminOrderActionResponse>
{
    private readonly IAdminOrderService _orders;

    public UpdateAdminOrderLineEndpoint(IAdminOrderService orders) => _orders = orders;

    public override void Configure()
    {
        Put("admin/orders/{orderId}/line-item");
        Policies(AuthPolicies.Employee);
    }

    public override async Task HandleAsync(UpdateOrderLineRequest req, CancellationToken ct)
    {
        if (!int.TryParse(Route<string>("orderId"), out var orderId) || orderId <= 0)
        {
            ThrowError("Invalid order id.", StatusCodes.Status400BadRequest);
            return;
        }

        try
        {
            var result = await _orders.UpdateOrderLineAsync(orderId, req, ct);
            await Send.OkAsync(result, ct);
        }
        catch (InvalidOperationException ex)
        {
            ThrowError(ex.Message, StatusCodes.Status400BadRequest);
        }
    }
}
