using FastEndpoints;
using IdealWeightNutrition.Application.Abstractions;
using IdealWeightNutrition.Contracts.Newsletter;
using AuthPolicies = IdealWeightNutrition.Domain.Constants.Policies;

namespace IdealWeightNutrition.Api.Features.Admin;

public sealed class ListAdminNewsletterRequest
{
    public string? Status { get; init; } = "all";
}

public sealed class ListAdminNewsletterEndpoint : Endpoint<ListAdminNewsletterRequest, IReadOnlyList<NewsletterSubscriptionDto>>
{
    private readonly INewsletterService _newsletter;

    public ListAdminNewsletterEndpoint(INewsletterService newsletter) => _newsletter = newsletter;

    public override void Configure()
    {
        Get("admin/newsletter");
        Policies(AuthPolicies.Admin);
    }

    public override async Task HandleAsync(ListAdminNewsletterRequest req, CancellationToken ct) =>
        await Send.OkAsync(await _newsletter.ListSubscriptionsAsync(req.Status, ct), ct);
}

public sealed class ToggleAdminNewsletterEndpoint : EndpointWithoutRequest
{
    private readonly INewsletterService _newsletter;

    public ToggleAdminNewsletterEndpoint(INewsletterService newsletter) => _newsletter = newsletter;

    public override void Configure()
    {
        Post("admin/newsletter/{id}/toggle");
        Policies(AuthPolicies.Admin);
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        if (!int.TryParse(Route<string>("id"), out var id) || id <= 0)
        {
            ThrowError("Invalid subscription id.", StatusCodes.Status400BadRequest);
            return;
        }

        try
        {
            await _newsletter.ToggleActiveAsync(id, ct);
            await Send.NoContentAsync(ct);
        }
        catch (InvalidOperationException ex)
        {
            ThrowError(ex.Message, StatusCodes.Status404NotFound);
        }
    }
}

public sealed class DeleteAdminNewsletterEndpoint : EndpointWithoutRequest
{
    private readonly INewsletterService _newsletter;

    public DeleteAdminNewsletterEndpoint(INewsletterService newsletter) => _newsletter = newsletter;

    public override void Configure()
    {
        Delete("admin/newsletter/{id}");
        Policies(AuthPolicies.Admin);
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        if (!int.TryParse(Route<string>("id"), out var id) || id <= 0)
        {
            ThrowError("Invalid subscription id.", StatusCodes.Status400BadRequest);
            return;
        }

        try
        {
            await _newsletter.DeleteAsync(id, ct);
            await Send.NoContentAsync(ct);
        }
        catch (InvalidOperationException ex)
        {
            ThrowError(ex.Message, StatusCodes.Status404NotFound);
        }
    }
}

public sealed class ExportAdminNewsletterEndpoint : EndpointWithoutRequest
{
    private readonly INewsletterService _newsletter;
    private readonly IDateTimeProvider _clock;

    public ExportAdminNewsletterEndpoint(INewsletterService newsletter, IDateTimeProvider clock)
    {
        _newsletter = newsletter;
        _clock = clock;
    }

    public override void Configure()
    {
        Get("admin/newsletter/export");
        Policies(AuthPolicies.Admin);
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var bytes = await _newsletter.ExportActiveCsvAsync(ct);
        var fileName = $"newsletter-subscribers-{_clock.Now:yyyyMMdd-HHmmss}.csv";
        await Send.BytesAsync(bytes, fileName, "text/csv", cancellation: ct);
    }
}
