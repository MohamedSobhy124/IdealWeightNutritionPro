using FastEndpoints;
using IdealWeightNutrition.Domain.Time;

namespace IdealWeightNutrition.Api.Features.Health;

public sealed class GetHealthEndpoint : EndpointWithoutRequest
{
    public override void Configure()
    {
        Get("health");
        AllowAnonymous();
        Summary(summary =>
        {
            summary.Summary = "API health check endpoint.";
            summary.Description = "Returns a simple healthy status payload.";
        });
    }

    public override Task HandleAsync(CancellationToken ct)
    {
        return Send.OkAsync(new
        {
            status = "healthy",
            service = "IdealWeightNutrition.Api",
            uae = UaeDateTime.Now,
            utc = DateTime.UtcNow
        }, ct);
    }
}
