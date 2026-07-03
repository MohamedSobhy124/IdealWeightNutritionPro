using FastEndpoints;
using IdealWeightNutrition.Contracts.Content;
using IdealWeightNutrition.Infrastructure.Options;
using Microsoft.Extensions.Options;

namespace IdealWeightNutrition.Api.Features.Content;

public sealed class GetCallSettingsEndpoint : EndpointWithoutRequest<CallSettingsDto>
{
    private readonly CallOptions _options;

    public GetCallSettingsEndpoint(IOptions<CallOptions> options) => _options = options.Value;

    public override void Configure()
    {
        Get("content/call");
        AllowAnonymous();
    }

    public override Task HandleAsync(CancellationToken ct) =>
        Send.OkAsync(new CallSettingsDto
        {
            Enabled = _options.Enabled,
            PhoneNumber = _options.PhoneNumber
        }, ct);
}
