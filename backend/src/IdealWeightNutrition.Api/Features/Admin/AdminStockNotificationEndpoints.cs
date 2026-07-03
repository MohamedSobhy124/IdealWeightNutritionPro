using FastEndpoints;
using IdealWeightNutrition.Application.Abstractions;
using IdealWeightNutrition.Contracts.Admin;
using AuthPolicies = IdealWeightNutrition.Domain.Constants.Policies;

namespace IdealWeightNutrition.Api.Features.Admin;

public sealed class ListAdminStockNotificationsRequest
{
    public bool ActiveOnly { get; init; } = true;
    public bool PendingOnly { get; init; }
    public string? Search { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 25;
}

public sealed class ListAdminStockNotificationsEndpoint : Endpoint<ListAdminStockNotificationsRequest, AdminStockNotificationListResponse>
{
    private readonly IAdminStockNotificationService _notifications;

    public ListAdminStockNotificationsEndpoint(IAdminStockNotificationService notifications) =>
        _notifications = notifications;

    public override void Configure()
    {
        Get("admin/stock-notifications");
        Policies(AuthPolicies.Employee);
    }

    public override async Task HandleAsync(ListAdminStockNotificationsRequest req, CancellationToken ct) =>
        await Send.OkAsync(await _notifications.ListAsync(MapQuery(req), ct), ct);

    private static AdminStockNotificationQuery MapQuery(ListAdminStockNotificationsRequest req) => new()
    {
        ActiveOnly = req.ActiveOnly,
        PendingOnly = req.PendingOnly,
        Search = req.Search,
        Page = req.Page,
        PageSize = req.PageSize
    };
}

public sealed class DeactivateAdminStockNotificationEndpoint : EndpointWithoutRequest<AdminStockNotificationActionResponse>
{
    private readonly IAdminStockNotificationService _notifications;

    public DeactivateAdminStockNotificationEndpoint(IAdminStockNotificationService notifications) =>
        _notifications = notifications;

    public override void Configure()
    {
        Post("admin/stock-notifications/{id}/deactivate");
        Policies(AuthPolicies.Employee);
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        if (!int.TryParse(Route<string>("id"), out var id) || id <= 0)
        {
            ThrowError("Invalid notification id.", StatusCodes.Status400BadRequest);
            return;
        }

        try
        {
            var result = await _notifications.DeactivateAsync(id, ct);
            await Send.OkAsync(result, ct);
        }
        catch (InvalidOperationException ex)
        {
            ThrowError(ex.Message, StatusCodes.Status404NotFound);
        }
    }
}
