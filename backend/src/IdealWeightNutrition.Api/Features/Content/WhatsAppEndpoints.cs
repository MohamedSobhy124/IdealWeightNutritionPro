using FastEndpoints;
using IdealWeightNutrition.Contracts.Content;
using IdealWeightNutrition.Infrastructure.Options;
using Microsoft.Extensions.Options;

namespace IdealWeightNutrition.Api.Features.Content;

public sealed class GetWhatsAppSettingsEndpoint : EndpointWithoutRequest<WhatsAppSettingsDto>
{
    private readonly WhatsAppOptions _options;

    public GetWhatsAppSettingsEndpoint(IOptions<WhatsAppOptions> options) => _options = options.Value;

    public override void Configure()
    {
        Get("content/whatsapp");
        AllowAnonymous();
    }

    public override Task HandleAsync(CancellationToken ct) =>
        Send.OkAsync(new WhatsAppSettingsDto
        {
            Enabled = _options.Enabled,
            PhoneNumber = _options.PhoneNumber,
            DefaultMessage = _options.DefaultMessage,
            DefaultMessageAr = _options.DefaultMessageAr
        }, ct);
}
