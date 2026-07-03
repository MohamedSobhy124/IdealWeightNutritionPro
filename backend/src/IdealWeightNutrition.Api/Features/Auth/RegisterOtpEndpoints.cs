using FastEndpoints;
using IdealWeightNutrition.Api.Http;
using IdealWeightNutrition.Application.Abstractions;
using IdealWeightNutrition.Contracts.Auth;

namespace IdealWeightNutrition.Api.Features.Auth;

public sealed class SendRegistrationOtpEndpoint : Endpoint<SendRegistrationOtpRequest, MessageResponse>
{
    private readonly IAuthService _auth;

    public SendRegistrationOtpEndpoint(IAuthService auth) => _auth = auth;

    public override void Configure()
    {
        Post("auth/register/send-otp");
        AllowAnonymous();
        Options(o => o.RequireRateLimiting(RateLimitPolicies.Auth));
    }

    public override async Task HandleAsync(SendRegistrationOtpRequest req, CancellationToken ct)
    {
        var result = await _auth.SendRegistrationOtpAsync(req.Email, ct);
        if (!result.Succeeded)
            ThrowError(result.Error!, result.StatusCode);

        await Send.OkAsync(result.Value!, ct);
    }
}

public sealed class VerifyRegistrationOtpEndpoint : Endpoint<VerifyOtpRequest, MessageResponse>
{
    private readonly IAuthService _auth;

    public VerifyRegistrationOtpEndpoint(IAuthService auth) => _auth = auth;

    public override void Configure()
    {
        Post("auth/register/verify-otp");
        AllowAnonymous();
        Options(o => o.RequireRateLimiting(RateLimitPolicies.Auth));
    }

    public override async Task HandleAsync(VerifyOtpRequest req, CancellationToken ct)
    {
        var result = await _auth.VerifyRegistrationOtpAsync(req.Email, req.Otp, ct);
        if (!result.Succeeded)
            ThrowError(result.Error!, result.StatusCode);

        await Send.OkAsync(result.Value!, ct);
    }
}

public sealed class SendRegistrationOtpRequest
{
    public required string Email { get; init; }
}
