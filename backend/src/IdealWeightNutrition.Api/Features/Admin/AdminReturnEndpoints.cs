using FastEndpoints;
using IdealWeightNutrition.Application.Abstractions;
using IdealWeightNutrition.Contracts.Returns;
using AuthPolicies = IdealWeightNutrition.Domain.Constants.Policies;

namespace IdealWeightNutrition.Api.Features.Admin;

public sealed class ListAdminReturnsRequest
{
    public string? Status { get; init; }
}

public sealed class ListAdminReturnsEndpoint : Endpoint<ListAdminReturnsRequest, IReadOnlyList<ReturnListItemDto>>
{
    private readonly IReturnService _returns;

    public ListAdminReturnsEndpoint(IReturnService returns) => _returns = returns;

    public override void Configure()
    {
        Get("admin/returns");
        Policies(AuthPolicies.Employee);
    }

    public override async Task HandleAsync(ListAdminReturnsRequest req, CancellationToken ct)
    {
        var items = await _returns.ListAdminReturnsAsync(req.Status, ct);
        await Send.OkAsync(items, ct);
    }
}

public sealed class GetAdminReturnEndpoint : EndpointWithoutRequest<ReturnRequestDto>
{
    private readonly IReturnService _returns;

    public GetAdminReturnEndpoint(IReturnService returns) => _returns = returns;

    public override void Configure()
    {
        Get("admin/returns/{returnId}");
        Policies(AuthPolicies.Employee);
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        if (!int.TryParse(Route<string>("returnId"), out var returnId) || returnId <= 0)
        {
            ThrowError("Invalid return id.", StatusCodes.Status400BadRequest);
            return;
        }

        var result = await _returns.GetAdminReturnAsync(returnId, ct);
        if (result is null)
            ThrowError("Return request not found.", StatusCodes.Status404NotFound);
        else
            await Send.OkAsync(result, ct);
    }
}

public sealed class ApproveReturnEndpoint : Endpoint<ApproveReturnRequest, ReturnActionResponse>
{
    private readonly IReturnService _returns;

    public ApproveReturnEndpoint(IReturnService returns) => _returns = returns;

    public override void Configure()
    {
        Post("admin/returns/{returnId}/approve");
        Policies(AuthPolicies.Employee);
    }

    public override async Task HandleAsync(ApproveReturnRequest req, CancellationToken ct)
    {
        var returnId = ParseReturnId();
        try
        {
            var result = await _returns.ApproveReturnAsync(returnId, req, ct);
            await Send.OkAsync(result, ct);
        }
        catch (InvalidOperationException ex)
        {
            ThrowError(ex.Message, StatusCodes.Status400BadRequest);
        }
    }

    private int ParseReturnId()
    {
        if (!int.TryParse(Route<string>("returnId"), out var returnId) || returnId <= 0)
            ThrowError("Invalid return id.", StatusCodes.Status400BadRequest);
        return returnId;
    }
}

public sealed class RejectReturnEndpoint : Endpoint<RejectReturnRequest, ReturnActionResponse>
{
    private readonly IReturnService _returns;

    public RejectReturnEndpoint(IReturnService returns) => _returns = returns;

    public override void Configure()
    {
        Post("admin/returns/{returnId}/reject");
        Policies(AuthPolicies.Employee);
    }

    public override async Task HandleAsync(RejectReturnRequest req, CancellationToken ct)
    {
        if (!int.TryParse(Route<string>("returnId"), out var returnId) || returnId <= 0)
        {
            ThrowError("Invalid return id.", StatusCodes.Status400BadRequest);
            return;
        }

        try
        {
            var result = await _returns.RejectReturnAsync(returnId, req, ct);
            await Send.OkAsync(result, ct);
        }
        catch (InvalidOperationException ex)
        {
            ThrowError(ex.Message, StatusCodes.Status400BadRequest);
        }
    }
}

public sealed class MarkReturnReceivedEndpoint : EndpointWithoutRequest<ReturnActionResponse>
{
    private readonly IReturnService _returns;

    public MarkReturnReceivedEndpoint(IReturnService returns) => _returns = returns;

    public override void Configure()
    {
        Post("admin/returns/{returnId}/receive");
        Policies(AuthPolicies.Employee);
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        if (!int.TryParse(Route<string>("returnId"), out var returnId) || returnId <= 0)
        {
            ThrowError("Invalid return id.", StatusCodes.Status400BadRequest);
            return;
        }

        try
        {
            var result = await _returns.MarkReturnReceivedAsync(returnId, ct);
            await Send.OkAsync(result, ct);
        }
        catch (InvalidOperationException ex)
        {
            ThrowError(ex.Message, StatusCodes.Status400BadRequest);
        }
    }
}

public sealed class CompleteReturnEndpoint : Endpoint<CompleteReturnRequest, ReturnActionResponse>
{
    private readonly IReturnService _returns;

    public CompleteReturnEndpoint(IReturnService returns) => _returns = returns;

    public override void Configure()
    {
        Post("admin/returns/{returnId}/complete");
        Policies(AuthPolicies.Employee);
    }

    public override async Task HandleAsync(CompleteReturnRequest req, CancellationToken ct)
    {
        if (!int.TryParse(Route<string>("returnId"), out var returnId) || returnId <= 0)
        {
            ThrowError("Invalid return id.", StatusCodes.Status400BadRequest);
            return;
        }

        try
        {
            var result = await _returns.CompleteReturnAsync(returnId, req, ct);
            await Send.OkAsync(result, ct);
        }
        catch (InvalidOperationException ex)
        {
            ThrowError(ex.Message, StatusCodes.Status400BadRequest);
        }
    }
}

public sealed class CancelReturnEndpoint : Endpoint<CancelReturnRequest, ReturnActionResponse>
{
    private readonly IReturnService _returns;

    public CancelReturnEndpoint(IReturnService returns) => _returns = returns;

    public override void Configure()
    {
        Post("admin/returns/{returnId}/cancel");
        Policies(AuthPolicies.Employee);
    }

    public override async Task HandleAsync(CancelReturnRequest req, CancellationToken ct)
    {
        if (!int.TryParse(Route<string>("returnId"), out var returnId) || returnId <= 0)
        {
            ThrowError("Invalid return id.", StatusCodes.Status400BadRequest);
            return;
        }

        try
        {
            var result = await _returns.CancelReturnAsync(returnId, req, ct);
            await Send.OkAsync(result, ct);
        }
        catch (InvalidOperationException ex)
        {
            ThrowError(ex.Message, StatusCodes.Status400BadRequest);
        }
    }
}

public sealed class RefundOrderEndpoint : Endpoint<RefundOrderRequest, RefundOrderResponse>
{
    private readonly IAdminOrderService _orders;

    public RefundOrderEndpoint(IAdminOrderService orders) => _orders = orders;

    public override void Configure()
    {
        Post("admin/orders/{orderId}/refund");
        Policies(AuthPolicies.Employee);
    }

    public override async Task HandleAsync(RefundOrderRequest req, CancellationToken ct)
    {
        if (!int.TryParse(Route<string>("orderId"), out var orderId) || orderId <= 0)
        {
            ThrowError("Invalid order id.", StatusCodes.Status400BadRequest);
            return;
        }

        try
        {
            var result = await _orders.RefundOrderAsync(orderId, req, ct);
            await Send.OkAsync(result, ct);
        }
        catch (InvalidOperationException ex)
        {
            ThrowError(ex.Message, StatusCodes.Status400BadRequest);
        }
    }
}
