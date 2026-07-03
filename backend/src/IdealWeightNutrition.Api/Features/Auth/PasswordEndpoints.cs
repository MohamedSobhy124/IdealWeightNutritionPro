using FastEndpoints;
using IdealWeightNutrition.Api.Http;
using IdealWeightNutrition.Application.Abstractions;
using IdealWeightNutrition.Contracts.Auth;
using System.Security.Claims;

namespace IdealWeightNutrition.Api.Features.Auth;

public sealed class ForgotPasswordEndpoint : Endpoint<ForgotPasswordRequest, MessageResponse>
{
    private readonly IAuthService _auth;

    public ForgotPasswordEndpoint(IAuthService auth) => _auth = auth;

    public override void Configure()
    {
        Post("auth/forgot-password");
        AllowAnonymous();
        Options(o => o.RequireRateLimiting(RateLimitPolicies.Auth));
        Summary(s => s.Summary = "Request a password reset email.");
    }

    public override async Task HandleAsync(ForgotPasswordRequest req, CancellationToken ct)
    {
        var result = await _auth.RequestPasswordResetAsync(req.Email, ct);
        if (!result.Succeeded)
            ThrowError(result.Error!, result.StatusCode);

        await Send.OkAsync(result.Value!, ct);
    }
}

public sealed class ResetPasswordEndpoint : Endpoint<ResetPasswordRequest, MessageResponse>
{
    private readonly IAuthService _auth;

    public ResetPasswordEndpoint(IAuthService auth) => _auth = auth;

    public override void Configure()
    {
        Post("auth/reset-password");
        AllowAnonymous();
        Options(o => o.RequireRateLimiting(RateLimitPolicies.Auth));
        Summary(s => s.Summary = "Set a new password using a reset token.");
    }

    public override async Task HandleAsync(ResetPasswordRequest req, CancellationToken ct)
    {
        var result = await _auth.ResetPasswordAsync(req, ct);
        if (!result.Succeeded)
            ThrowError(result.Error!, result.StatusCode);

        await Send.OkAsync(result.Value!, ct);
    }
}

public sealed class ChangePasswordEndpoint : Endpoint<ChangePasswordRequest, MessageResponse>
{
    private readonly IAuthService _auth;

    public ChangePasswordEndpoint(IAuthService auth) => _auth = auth;

    public override void Configure()
    {
        Post("auth/change-password");
        Summary(s => s.Summary = "Change password for logged-in user.");
    }

    public override async Task HandleAsync(ChangePasswordRequest req, CancellationToken ct)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            await Send.UnauthorizedAsync(ct);
            return;
        }

        var result = await _auth.ChangePasswordAsync(userId, req, ct);
        if (!result.Succeeded)
            ThrowError(result.Error!, result.StatusCode);
        await Send.OkAsync(result.Value!, ct);
    }
}

public sealed class ExportPersonalDataEndpoint : EndpointWithoutRequest<PersonalDataExportResponse>
{
    private readonly IAuthService _auth;

    public ExportPersonalDataEndpoint(IAuthService auth) => _auth = auth;

    public override void Configure()
    {
        Get("auth/personal-data");
        Summary(s => s.Summary = "Export personal account data.");
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            await Send.UnauthorizedAsync(ct);
            return;
        }
        var result = await _auth.ExportPersonalDataAsync(userId, ct);
        if (!result.Succeeded)
            ThrowError(result.Error!, result.StatusCode);
        await Send.OkAsync(result.Value!, ct);
    }
}

public sealed class DeletePersonalDataEndpoint : EndpointWithoutRequest<MessageResponse>
{
    private readonly IAuthService _auth;

    public DeletePersonalDataEndpoint(IAuthService auth) => _auth = auth;

    public override void Configure()
    {
        Delete("auth/personal-data");
        Summary(s => s.Summary = "Delete personal account data.");
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            await Send.UnauthorizedAsync(ct);
            return;
        }
        var result = await _auth.DeletePersonalDataAsync(userId, ct);
        if (!result.Succeeded)
            ThrowError(result.Error!, result.StatusCode);
        await Send.OkAsync(result.Value!, ct);
    }
}
