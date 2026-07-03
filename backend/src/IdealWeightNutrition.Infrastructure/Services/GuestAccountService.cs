using System.Security.Cryptography;
using System.Text;
using IdealWeightNutrition.Application.Abstractions;
using IdealWeightNutrition.Domain.Constants;
using IdealWeightNutrition.Domain.Identity;
using IdealWeightNutrition.Infrastructure.Options;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace IdealWeightNutrition.Infrastructure.Services;

internal sealed class GuestAccountService : IGuestAccountService
{
    private readonly UserManager<ApplicationUser> _users;
    private readonly IEmailService _email;
    private readonly AppUrlOptions _urls;
    private readonly ILogger<GuestAccountService> _logger;

    public GuestAccountService(
        UserManager<ApplicationUser> users,
        IEmailService email,
        IOptions<AppUrlOptions> urls,
        ILogger<GuestAccountService> logger)
    {
        _users = users;
        _email = email;
        _urls = urls.Value;
        _logger = logger;
    }

    public async Task<GuestAccountResult> ResolveOrCreateAsync(
        string email,
        string fullName,
        string? phoneNumber,
        string? streetAddress,
        string? city,
        string? state,
        string? postalCode,
        bool createAccountIfMissing,
        CancellationToken cancellationToken = default)
    {
        var normalizedEmail = email.Trim().ToLowerInvariant();

        var existing = await _users.Users
            .FirstOrDefaultAsync(u => u.Email != null && u.Email.ToLower() == normalizedEmail, cancellationToken);

        if (existing is not null)
        {
            return new GuestAccountResult
            {
                UserId = existing.Id,
                LinkedExistingAccount = true
            };
        }

        if (!createAccountIfMissing)
            return new GuestAccountResult();

        var user = new ApplicationUser
        {
            UserName = normalizedEmail,
            Email = normalizedEmail,
            Name = fullName.Trim(),
            PhoneNumber = phoneNumber?.Trim(),
            StreetAddress = streetAddress?.Trim(),
            City = city?.Trim(),
            State = string.IsNullOrWhiteSpace(state) ? "UAE" : state.Trim(),
            PostalCode = string.IsNullOrWhiteSpace(postalCode) ? "00000" : postalCode.Trim(),
            EmailConfirmed = true
        };

        var password = GenerateTemporaryPassword();
        var create = await _users.CreateAsync(user, password);
        if (!create.Succeeded)
        {
            _logger.LogWarning(
                "Guest account creation failed for {Email}: {Errors}",
                normalizedEmail,
                string.Join("; ", create.Errors.Select(e => e.Description)));
            return new GuestAccountResult();
        }

        await _users.AddToRoleAsync(user, Roles.Customer);
        await SendSetPasswordEmailAsync(user, cancellationToken);

        return new GuestAccountResult
        {
            UserId = user.Id,
            CreatedNewAccount = true
        };
    }

    private async Task SendSetPasswordEmailAsync(ApplicationUser user, CancellationToken cancellationToken)
    {
        try
        {
            var token = await _users.GeneratePasswordResetTokenAsync(user);
            var encoded = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));
            var email = user.Email ?? string.Empty;
            var link =
                $"{_urls.FrontendBaseUrl.TrimEnd('/')}/auth/reset-password?email={Uri.EscapeDataString(email)}&token={encoded}";

            var body = $"""
                <p>Welcome to Ideal Weight Nutrition!</p>
                <p>Your account has been created. Set your password to sign in and track orders:</p>
                <p><a href="{link}">Set your password</a></p>
                """;

            await _email.SendAsync(email, "Set your Ideal Weight Nutrition password", body, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to send set-password email for {Email}", user.Email);
        }
    }

    private static string GenerateTemporaryPassword()
    {
        var core = Convert.ToHexString(RandomNumberGenerator.GetBytes(8));
        return $"{core}Aa1!";
    }
}
