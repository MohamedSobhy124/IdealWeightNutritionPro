using FastEndpoints;
using IdealWeightNutrition.Application.Abstractions;
using IdealWeightNutrition.Contracts.Admin;
using AuthPolicies = IdealWeightNutrition.Domain.Constants.Policies;

namespace IdealWeightNutrition.Api.Features.Admin;

public sealed class ListAdminServicePurchasesRequest
{
    public string? PaymentStatus { get; init; }
    public string? ServiceStatus { get; init; }
    public DateTime? DateFrom { get; init; }
    public DateTime? DateTo { get; init; }
    public string? Search { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 25;
}

public sealed class ListAdminServicePurchasesEndpoint : Endpoint<ListAdminServicePurchasesRequest, AdminServicePurchaseListResponse>
{
    private readonly IAdminServicePurchaseService _purchases;

    public ListAdminServicePurchasesEndpoint(IAdminServicePurchaseService purchases) => _purchases = purchases;

    public override void Configure()
    {
        Get("admin/service-purchases");
        Policies(AuthPolicies.Admin);
    }

    public override async Task HandleAsync(ListAdminServicePurchasesRequest req, CancellationToken ct) =>
        await Send.OkAsync(await _purchases.ListAsync(MapQuery(req), ct), ct);

    private static AdminServicePurchaseQuery MapQuery(ListAdminServicePurchasesRequest req) => new()
    {
        PaymentStatus = req.PaymentStatus,
        ServiceStatus = req.ServiceStatus,
        DateFrom = req.DateFrom,
        DateTo = req.DateTo,
        Search = req.Search,
        Page = req.Page,
        PageSize = req.PageSize
    };
}

public sealed class GetAdminServicePurchaseEndpoint : EndpointWithoutRequest<AdminServicePurchaseDetailDto>
{
    private readonly IAdminServicePurchaseService _purchases;

    public GetAdminServicePurchaseEndpoint(IAdminServicePurchaseService purchases) => _purchases = purchases;

    public override void Configure()
    {
        Get("admin/service-purchases/{purchaseId}");
        Policies(AuthPolicies.Admin);
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        if (!int.TryParse(Route<string>("purchaseId"), out var id) || id <= 0)
        {
            ThrowError("Invalid purchase id.", StatusCodes.Status400BadRequest);
            return;
        }

        var purchase = await _purchases.GetAsync(id, ct);
        if (purchase is null)
            ThrowError("Service purchase not found.", StatusCodes.Status404NotFound);
        else
            await Send.OkAsync(purchase, ct);
    }
}

public sealed class ExportAdminServicePurchasesEndpoint : Endpoint<ListAdminServicePurchasesRequest>
{
    private readonly IAdminServicePurchaseService _purchases;
    private readonly IDateTimeProvider _clock;

    public ExportAdminServicePurchasesEndpoint(
        IAdminServicePurchaseService purchases,
        IDateTimeProvider clock)
    {
        _purchases = purchases;
        _clock = clock;
    }

    public override void Configure()
    {
        Get("admin/service-purchases/export");
        Policies(AuthPolicies.Admin);
    }

    public override async Task HandleAsync(ListAdminServicePurchasesRequest req, CancellationToken ct)
    {
        var bytes = await _purchases.ExportCsvAsync(new AdminServicePurchaseQuery
        {
            PaymentStatus = req.PaymentStatus,
            ServiceStatus = req.ServiceStatus,
            DateFrom = req.DateFrom,
            DateTo = req.DateTo,
            Search = req.Search,
            Page = 1,
            PageSize = int.MaxValue
        }, ct);

        var fileName = $"ServicePurchases_Export_{_clock.Now:yyyyMMdd_HHmmss}.csv";
        HttpContext.Response.ContentType = "text/csv";
        HttpContext.Response.Headers.ContentDisposition = $"attachment; filename=\"{fileName}\"";
        await HttpContext.Response.Body.WriteAsync(bytes, ct);
    }
}

public sealed class GetAdminServicePurchaseStatisticsEndpoint : EndpointWithoutRequest<AdminServicePurchaseStatisticsDto>
{
    private readonly IAdminServicePurchaseService _purchases;

    public GetAdminServicePurchaseStatisticsEndpoint(IAdminServicePurchaseService purchases) => _purchases = purchases;

    public override void Configure()
    {
        Get("admin/service-purchases/statistics");
        Policies(AuthPolicies.Admin);
    }

    public override async Task HandleAsync(CancellationToken ct) =>
        await Send.OkAsync(await _purchases.GetStatisticsAsync(ct), ct);
}

public sealed class UpdateAdminServicePurchaseEndpoint : Endpoint<UpdateAdminServicePurchaseRequest, AdminServicePurchaseActionResponse>
{
    private readonly IAdminServicePurchaseService _purchases;

    public UpdateAdminServicePurchaseEndpoint(IAdminServicePurchaseService purchases) => _purchases = purchases;

    public override void Configure()
    {
        Put("admin/service-purchases/{purchaseId}");
        Policies(AuthPolicies.Admin);
    }

    public override async Task HandleAsync(UpdateAdminServicePurchaseRequest req, CancellationToken ct)
    {
        if (!int.TryParse(Route<string>("purchaseId"), out var id) || id <= 0)
        {
            ThrowError("Invalid purchase id.", StatusCodes.Status400BadRequest);
            return;
        }

        try
        {
            var result = await _purchases.UpdateAsync(id, req, ct);
            await Send.OkAsync(result, ct);
        }
        catch (InvalidOperationException ex)
        {
            ThrowError(ex.Message, StatusCodes.Status400BadRequest);
        }
    }
}
