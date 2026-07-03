using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using IdealWeightNutrition.Application.Abstractions;
using IdealWeightNutrition.Contracts.Auth;
using IdealWeightNutrition.Domain.Constants;
using IdealWeightNutrition.Domain.Identity;
using IdealWeightNutrition.Infrastructure.Options;
using IdealWeightNutrition.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
namespace IdealWeightNutrition.Infrastructure.Services;

internal sealed class AuthService : IAuthService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly AppDbContext _db;
    private readonly JwtSettings _jwt;
    private readonly AppUrlOptions _urls;
    private readonly IEmailService _email;
    private readonly IOtpService _otp;
    private readonly IDateTimeProvider _clock;

    public AuthService(
        UserManager<ApplicationUser> userManager,
        AppDbContext db,
        IOptions<JwtSettings> jwt,
        IOptions<AppUrlOptions> urls,
        IEmailService email,
        IOtpService otp,
        IDateTimeProvider clock)
    {
        _userManager = userManager;
        _db = db;
        _jwt = jwt.Value;
        _urls = urls.Value;
        _email = email;
        _otp = otp;
        _clock = clock;
    }
    public async Task<AuthResult<AuthTokenResponse>> LoginAsync(
        LoginRequest request,
        CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user is null)
            return AuthResult<AuthTokenResponse>.Fail("Invalid email or password.", 401);

        if (await _userManager.IsLockedOutAsync(user))
            return AuthResult<AuthTokenResponse>.Fail("Account is locked. Try again later.", 423);

        if (!await _userManager.CheckPasswordAsync(user, request.Password))
        {
            await _userManager.AccessFailedAsync(user);
            return AuthResult<AuthTokenResponse>.Fail("Invalid email or password.", 401);
        }

        await _userManager.ResetAccessFailedCountAsync(user);
        return await IssueTokensAsync(user, cancellationToken);
    }

    public async Task<AuthResult<AuthTokenResponse>> RegisterAsync(
        RegisterRequest request,
        CancellationToken cancellationToken = default)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        if (!await _otp.IsEmailVerifiedAsync(email, OtpPurpose.Registration, cancellationToken))
            return AuthResult<AuthTokenResponse>.Fail("Please verify your email address before registering.");

        var existing = await _userManager.FindByEmailAsync(email);
        if (existing is not null)
            return AuthResult<AuthTokenResponse>.Fail("Email is already registered.");

        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            Name = request.FullName,
            PhoneNumber = request.Phone,
            EmailConfirmed = true
        };

        var create = await _userManager.CreateAsync(user, request.Password);
        if (!create.Succeeded)
        {
            var message = string.Join("; ", create.Errors.Select(e => e.Description));
            return AuthResult<AuthTokenResponse>.Fail(message);
        }

        await _userManager.AddToRoleAsync(user, Roles.Customer);
        return await IssueTokensAsync(user, cancellationToken);
    }

    public async Task<AuthResult<MessageResponse>> SendRegistrationOtpAsync(
        string email,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(email))
            return AuthResult<MessageResponse>.Fail("Email is required.");

        var normalized = email.Trim().ToLowerInvariant();
        if (!normalized.Contains('@'))
            return AuthResult<MessageResponse>.Fail("Enter a valid email address.");

        var existing = await _userManager.FindByEmailAsync(normalized);
        if (existing is not null)
            return AuthResult<MessageResponse>.Fail("This email is already registered. Please sign in instead.");

        var otp = _otp.GenerateOtp();
        await _otp.StoreOtpAsync(normalized, otp, OtpPurpose.Registration, cancellationToken);

        var body = $"""
            <p>Thank you for registering with Ideal Weight Nutrition.</p>
            <p>Your verification code is:</p>
            <p style="font-size:24px;font-weight:bold;letter-spacing:4px;">{otp}</p>
            <p>This code expires in 10 minutes.</p>
            """;
        await _email.SendAsync(normalized, "Verify your email — Ideal Weight Nutrition", body, cancellationToken);

        return AuthResult<MessageResponse>.Ok(new MessageResponse
        {
            Message = "Verification code sent to your email."
        });
    }

    public async Task<AuthResult<MessageResponse>> VerifyRegistrationOtpAsync(
        string email,
        string otp,
        CancellationToken cancellationToken = default)
    {
        var result = await _otp.VerifyOtpAsync(email, otp, OtpPurpose.Registration, cancellationToken);
        if (!result.IsValid)
            return AuthResult<MessageResponse>.Fail(result.Message ?? "Invalid verification code.");

        return AuthResult<MessageResponse>.Ok(new MessageResponse
        {
            Message = result.Message ?? "Email verified successfully."
        });
    }

    public async Task<AuthResult<AuthTokenResponse>> RefreshAsync(
        RefreshTokenRequest request,
        CancellationToken cancellationToken = default)
    {
        var hash = HashToken(request.RefreshToken);
        var stored = await _db.RefreshTokens
            .FirstOrDefaultAsync(x => x.TokenHash == hash, cancellationToken);

        if (stored is null || !stored.IsActive)
            return AuthResult<AuthTokenResponse>.Fail("Invalid refresh token.", 401);

        var user = await _userManager.FindByIdAsync(stored.UserId);
        if (user is null)
            return AuthResult<AuthTokenResponse>.Fail("Invalid refresh token.", 401);

        stored.RevokedAt = _clock.Now;
        var newTokens = await IssueTokensAsync(user, cancellationToken);
        if (newTokens.Succeeded && newTokens.Value is not null)
            stored.ReplacedByTokenHash = HashToken(newTokens.Value.RefreshToken);

        await _db.SaveChangesAsync(cancellationToken);
        return newTokens;
    }

    public async Task<AuthResult<UserProfileResponse>> GetProfileAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user is null)
            return AuthResult<UserProfileResponse>.Fail("User not found.", 404);

        var roles = await _userManager.GetRolesAsync(user);
        return AuthResult<UserProfileResponse>.Ok(new UserProfileResponse
        {
            Id = user.Id,
            Email = user.Email ?? string.Empty,
            FullName = user.Name,
            Roles = roles.ToList()
        });
    }

    public async Task RevokeRefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        var hash = HashToken(refreshToken);
        var stored = await _db.RefreshTokens
            .FirstOrDefaultAsync(x => x.TokenHash == hash, cancellationToken);

        if (stored is not null && stored.RevokedAt is null)
        {
            stored.RevokedAt = _clock.Now;
            await _db.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<AuthResult<MessageResponse>> RequestPasswordResetAsync(
        string email,
        CancellationToken cancellationToken = default)
    {
        var normalized = email.Trim().ToLowerInvariant();
        var user = await _userManager.FindByEmailAsync(normalized);
        if (user is not null)
        {
            try
            {
                var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                var encoded = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));
                var link =
                    $"{_urls.FrontendBaseUrl.TrimEnd('/')}/auth/reset-password?email={Uri.EscapeDataString(normalized)}&token={encoded}";
                var body = $"""
                    <p>Reset your Ideal Weight Nutrition password:</p>
                    <p><a href="{link}">Set a new password</a></p>
                    """;
                await _email.SendAsync(normalized, "Reset your password", body, cancellationToken);
            }
            catch
            {
                // Do not reveal whether email exists.
            }
        }

        return AuthResult<MessageResponse>.Ok(new MessageResponse
        {
            Message = "If an account exists for that email, a reset link has been sent."
        });
    }

    public async Task<AuthResult<MessageResponse>> ResetPasswordAsync(
        ResetPasswordRequest request,
        CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByEmailAsync(request.Email.Trim().ToLowerInvariant());
        if (user is null)
            return AuthResult<MessageResponse>.Fail("Invalid reset link.", 400);

        string token;
        try
        {
            token = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(request.Token));
        }
        catch
        {
            return AuthResult<MessageResponse>.Fail("Invalid reset link.", 400);
        }

        var result = await _userManager.ResetPasswordAsync(user, token, request.Password);
        if (!result.Succeeded)
        {
            var message = string.Join("; ", result.Errors.Select(e => e.Description));
            return AuthResult<MessageResponse>.Fail(message);
        }

        return AuthResult<MessageResponse>.Ok(new MessageResponse
        {
            Message = "Password updated. You can sign in now."
        });
    }

    public async Task<AuthResult<MessageResponse>> ChangePasswordAsync(
        string userId,
        ChangePasswordRequest request,
        CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user is null)
            return AuthResult<MessageResponse>.Fail("User not found.", 404);

        var result = await _userManager.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword);
        if (!result.Succeeded)
        {
            var message = string.Join("; ", result.Errors.Select(e => e.Description));
            return AuthResult<MessageResponse>.Fail(message);
        }

        return AuthResult<MessageResponse>.Ok(new MessageResponse { Message = "Password changed successfully." });
    }

    public async Task<AuthResult<PersonalDataExportResponse>> ExportPersonalDataAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user is null)
            return AuthResult<PersonalDataExportResponse>.Fail("User not found.", 404);

        var roles = await _userManager.GetRolesAsync(user);
        var orders = await _db.OrderHeaders.AsNoTracking()
            .Where(x => x.ApplicationUserId == userId)
            .Select(x => new { x.Id, x.OrderDate, x.OrderStatus, x.PaymentStatus, x.OrderTotal })
            .ToListAsync(cancellationToken);
        var returns = await _db.ReturnRequests.AsNoTracking()
            .Where(x => x.ApplicationUserId == userId)
            .Select(x => new { x.Id, x.OrderHeaderId, x.Status, x.RequestDate, x.RefundAmount })
            .ToListAsync(cancellationToken);
        var wishlist = await _db.WishlistItems.AsNoTracking()
            .Where(x => x.ApplicationUserId == userId)
            .Select(x => new { x.Id, x.ProductId })
            .ToListAsync(cancellationToken);

        var payload = JsonSerializer.Serialize(new
        {
            user = new { user.Id, user.Email, user.Name, user.PhoneNumber, roles },
            orders,
            returns,
            wishlist
        });
        return AuthResult<PersonalDataExportResponse>.Ok(new PersonalDataExportResponse { Json = payload });
    }

    public async Task<AuthResult<MessageResponse>> DeletePersonalDataAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user is null)
            return AuthResult<MessageResponse>.Fail("User not found.", 404);

        await using var tx = await _db.Database.BeginTransactionAsync(cancellationToken);
        _db.WishlistItems.RemoveRange(_db.WishlistItems.Where(x => x.ApplicationUserId == userId));
        _db.RefreshTokens.RemoveRange(_db.RefreshTokens.Where(x => x.UserId == userId));
        await _db.SaveChangesAsync(cancellationToken);

        var del = await _userManager.DeleteAsync(user);
        if (!del.Succeeded)
        {
            var message = string.Join("; ", del.Errors.Select(e => e.Description));
            return AuthResult<MessageResponse>.Fail(message);
        }

        await tx.CommitAsync(cancellationToken);
        return AuthResult<MessageResponse>.Ok(new MessageResponse { Message = "Account deleted." });
    }

    public async Task<AuthResult<AuthTokenResponse>> LoginWithGoogleAsync(
        ClaimsPrincipal principal,
        CancellationToken cancellationToken = default)
    {
        var email = principal.FindFirstValue(ClaimTypes.Email)
            ?? principal.FindFirstValue("email");
        var providerKey = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(providerKey))
            return AuthResult<AuthTokenResponse>.Fail("Google account did not return required profile information.", 400);

        email = email.Trim().ToLowerInvariant();
        const string provider = "Google";
        var user = await _userManager.FindByLoginAsync(provider, providerKey);
        if (user is null)
        {
            user = await _userManager.FindByEmailAsync(email);
            if (user is null)
            {
                var displayName = principal.FindFirstValue(ClaimTypes.Name) ?? email;
                user = new ApplicationUser
                {
                    UserName = email,
                    Email = email,
                    Name = displayName,
                    EmailConfirmed = true
                };

                var create = await _userManager.CreateAsync(user);
                if (!create.Succeeded)
                {
                    var message = string.Join("; ", create.Errors.Select(e => e.Description));
                    return AuthResult<AuthTokenResponse>.Fail(message);
                }

                await _userManager.AddToRoleAsync(user, Roles.Customer);
            }

            var loginResult = await _userManager.AddLoginAsync(user, new UserLoginInfo(provider, providerKey, provider));
            if (!loginResult.Succeeded)
            {
                var message = string.Join("; ", loginResult.Errors.Select(e => e.Description));
                return AuthResult<AuthTokenResponse>.Fail(message);
            }
        }

        return await IssueTokensAsync(user, cancellationToken);
    }

    private async Task<AuthResult<AuthTokenResponse>> IssueTokensAsync(
        ApplicationUser user,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_jwt.SigningKey) || _jwt.SigningKey.Length < 32)
            return AuthResult<AuthTokenResponse>.Fail("JWT signing key is not configured.", 500);

        var roles = await _userManager.GetRolesAsync(user);
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id),
            new(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
            new(ClaimTypes.NameIdentifier, user.Id),
            new(ClaimTypes.Name, user.Name ?? user.Email ?? string.Empty)
        };
        claims.AddRange(roles.Select(r => new Claim(ClaimTypes.Role, r)));

        var expiresAt = _clock.UtcNow.AddMinutes(_jwt.AccessTokenMinutes);
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwt.SigningKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var jwt = new JwtSecurityToken(
            issuer: _jwt.Issuer,
            audience: _jwt.Audience,
            claims: claims,
            expires: expiresAt,
            signingCredentials: creds);

        var accessToken = new JwtSecurityTokenHandler().WriteToken(jwt);
        var refreshToken = GenerateRefreshToken();
        var refreshHash = HashToken(refreshToken);

        _db.RefreshTokens.Add(new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            TokenHash = refreshHash,
            CreatedAt = _clock.Now,
            ExpiresAt = _clock.Now.AddDays(_jwt.RefreshTokenDays)
        });

        await _db.SaveChangesAsync(cancellationToken);

        return AuthResult<AuthTokenResponse>.Ok(new AuthTokenResponse
        {
            UserId = user.Id,
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            ExpiresAt = expiresAt
        });
    }

    private static string GenerateRefreshToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(64);
        return Convert.ToBase64String(bytes);
    }

    private static string HashToken(string token)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return Convert.ToHexString(hash);
    }
}
