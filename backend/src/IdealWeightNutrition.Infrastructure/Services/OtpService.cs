using System.Security.Cryptography;
using System.Text.Json;
using IdealWeightNutrition.Application.Abstractions;
using IdealWeightNutrition.Domain.Time;
using Microsoft.Extensions.Caching.Distributed;

namespace IdealWeightNutrition.Infrastructure.Services;

internal sealed class OtpService(IDistributedCache cache) : IOtpService
{
    private const int OtpExpiryMinutes = 10;
    private const int MaxAttempts = 5;
    private const int VerifiedTtlMinutes = 30;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public string GenerateOtp()
    {
        Span<byte> bytes = stackalloc byte[4];
        RandomNumberGenerator.Fill(bytes);
        var random = BitConverter.ToUInt32(bytes);
        return ((random % 900_000) + 100_000).ToString("D6");
    }

    public Task StoreOtpAsync(
        string email,
        string otp,
        OtpPurpose purpose = OtpPurpose.Checkout,
        CancellationToken cancellationToken = default)
    {
        var normalized = NormalizeEmail(email);
        var data = new OtpCacheEntry
        {
            Otp = otp,
            Email = normalized,
            CreatedAtUtc = UaeDateTime.Now,
            Attempts = 0
        };

        return cache.SetStringAsync(
            OtpKey(normalized, purpose),
            JsonSerializer.Serialize(data, JsonOptions),
            new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(OtpExpiryMinutes)
            },
            cancellationToken);
    }

    public async Task<OtpVerificationResult> VerifyOtpAsync(
        string email,
        string otp,
        OtpPurpose purpose = OtpPurpose.Checkout,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(otp))
        {
            return new OtpVerificationResult
            {
                IsValid = false,
                Message = "Email and OTP are required."
            };
        }

        var normalized = NormalizeEmail(email);
        var json = await cache.GetStringAsync(OtpKey(normalized, purpose), cancellationToken);
        if (string.IsNullOrEmpty(json))
        {
            return new OtpVerificationResult
            {
                IsValid = false,
                Message = "OTP has expired or was not found. Please request a new code."
            };
        }

        var data = JsonSerializer.Deserialize<OtpCacheEntry>(json, JsonOptions);
        if (data is null)
        {
            return new OtpVerificationResult
            {
                IsValid = false,
                Message = "OTP has expired or was not found. Please request a new code."
            };
        }

        if (data.Attempts >= MaxAttempts)
        {
            await cache.RemoveAsync(OtpKey(normalized, purpose), cancellationToken);
            return new OtpVerificationResult
            {
                IsValid = false,
                Message = "Too many failed attempts. Please request a new code."
            };
        }

        data.Attempts++;
        await cache.SetStringAsync(
            OtpKey(normalized, purpose),
            JsonSerializer.Serialize(data, JsonOptions),
            new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(OtpExpiryMinutes)
            },
            cancellationToken);

        if (!data.Otp.Equals(otp.Trim(), StringComparison.OrdinalIgnoreCase))
        {
            return new OtpVerificationResult
            {
                IsValid = false,
                Message = $"Invalid code. {MaxAttempts - data.Attempts} attempt(s) remaining."
            };
        }

        await cache.RemoveAsync(OtpKey(normalized, purpose), cancellationToken);
        await cache.SetStringAsync(
            VerifiedKey(normalized, purpose),
            "1",
            new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(VerifiedTtlMinutes)
            },
            cancellationToken);

        return new OtpVerificationResult
        {
            IsValid = true,
            Message = "Email verified successfully."
        };
    }

    public async Task<bool> IsEmailVerifiedAsync(
        string email,
        OtpPurpose purpose = OtpPurpose.Checkout,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(email))
            return false;

        var value = await cache.GetStringAsync(VerifiedKey(NormalizeEmail(email), purpose), cancellationToken);
        return !string.IsNullOrEmpty(value);
    }

    private static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();

    private static string OtpKey(string email, OtpPurpose purpose) => $"{purpose.ToString().ToLowerInvariant()}-otp:{email}";

    private static string VerifiedKey(string email, OtpPurpose purpose) => $"{purpose.ToString().ToLowerInvariant()}-verified:{email}";

    private sealed class OtpCacheEntry
    {
        public string Otp { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public DateTime CreatedAtUtc { get; set; }
        public int Attempts { get; set; }
    }
}
