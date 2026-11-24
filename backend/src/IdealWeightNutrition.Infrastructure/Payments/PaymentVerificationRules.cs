using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography;
using System.Text;
using IdealWeightNutrition.Utility;
using Microsoft.IdentityModel.Tokens;

namespace IdealWeightNutrition.Infrastructure.Payments;

/// <summary>
/// Pure rules for deciding whether a gateway's answer really confirms (or really fails) a
/// payment for one of our orders. Kept free of I/O so they can be unit tested.
/// Nothing a customer can edit (return-URL query strings, webhook bodies) is trusted here.
/// </summary>
public static class PaymentVerificationRules
{
    public const decimal AmountTolerance = 0.02m;

    public static bool AmountsMatch(decimal expected, decimal actual, decimal tolerance = AmountTolerance) =>
        Math.Abs(expected - actual) <= tolerance;

    // ---------------------------------------------------------------- Tabby

    /// <summary>
    /// True when the Tabby payment was created for <paramref name="expectedReference"/>.
    /// A payment id taken from the return URL (not one we stored) must carry a matching
    /// reference; a stored id is accepted unless Tabby reports a different reference.
    /// </summary>
    public static bool TabbyPaymentBelongsTo(
        TappyPaymentStatusResponse result,
        string expectedReference,
        bool requireReference)
    {
        if (!result.Success)
            return false;

        if (string.IsNullOrEmpty(result.OrderReferenceId))
            return !requireReference;

        return string.Equals(result.OrderReferenceId, expectedReference, StringComparison.Ordinal);
    }

    /// <summary>
    /// Paid only when Tabby reports a paid status, the payment belongs to this order and
    /// the amount matches the order total.
    /// </summary>
    public static (bool IsPaid, string? Message) CheckTabbyVerdict(
        TappyPaymentStatusResponse result,
        decimal expectedAmount,
        string expectedReference)
    {
        if (!result.Success || !result.IsPaid)
            return (false, result.Message);

        if (!TabbyPaymentBelongsTo(result, expectedReference, requireReference: false))
            return (false, "Tabby payment belongs to a different order.");

        if (result.Amount.HasValue && !AmountsMatch(expectedAmount, result.Amount.Value))
            return (false, "Payment amount does not match the order total.");

        return (true, result.Message);
    }

    // ---------------------------------------------------------------- Tamara

    /// <summary>
    /// Tamara sends the webhook token as <c>Authorization: Bearer &lt;token&gt;</c> and/or as
    /// the <c>tamaraToken</c> query parameter. Returns whichever is present.
    /// </summary>
    public static string? ExtractTamaraToken(string? authorizationHeader, string? queryToken)
    {
        if (!string.IsNullOrWhiteSpace(authorizationHeader)
            && authorizationHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            var token = authorizationHeader["Bearer ".Length..].Trim();
            if (token.Length > 0)
                return token;
        }

        return string.IsNullOrWhiteSpace(queryToken) ? null : queryToken.Trim();
    }

    /// <summary>
    /// Validates a Tamara webhook token. Tamara signs it as an HS256 JWT with the merchant's
    /// notification token, so the signature (and expiry, when present) is checked.
    /// Fails closed when no notification token is configured.
    /// </summary>
    public static bool IsValidTamaraWebhookToken(string? token, string? notificationToken)
    {
        if (string.IsNullOrWhiteSpace(notificationToken) || string.IsNullOrWhiteSpace(token))
            return false;

        // Legacy setups send the notification token itself. Anyone who knows it can sign a
        // JWT anyway, so accepting an exact (constant-time) match adds no exposure.
        var tokenBytes = Encoding.UTF8.GetBytes(token);
        var expectedBytes = Encoding.UTF8.GetBytes(notificationToken);
        if (tokenBytes.Length == expectedBytes.Length
            && CryptographicOperations.FixedTimeEquals(tokenBytes, expectedBytes))
            return true;

        try
        {
            var handler = new JwtSecurityTokenHandler { MapInboundClaims = false };
            handler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(expectedBytes),
                ValidAlgorithms = [SecurityAlgorithms.HmacSha256],
                ValidateIssuer = false,
                ValidateAudience = false,
                ValidateLifetime = true,
                RequireExpirationTime = false,
                RequireSignedTokens = true,
                ClockSkew = TimeSpan.FromMinutes(5)
            }, out _);
            return true;
        }
        catch (Exception ex) when (ex is SecurityTokenException or ArgumentException)
        {
            return false;
        }
    }

    /// <summary>
    /// True only when Tamara's API answered and reports <paramref name="expectedReference"/>
    /// as the order's merchant reference. A missing reference counts as "not confirmed".
    /// </summary>
    public static bool TamaraOrderBelongsTo(bool apiCallSucceeded, string? orderReferenceId, string expectedReference) =>
        apiCallSucceeded
        && !string.IsNullOrEmpty(orderReferenceId)
        && string.Equals(orderReferenceId.Trim(), expectedReference, StringComparison.Ordinal);

    /// <summary>
    /// A cancellation is acted on only when Tamara's own API (not the webhook body) says the
    /// order was cancelled, declined or expired.
    /// </summary>
    public static bool TamaraApiIndicatesCancelled(bool apiCallSucceeded, string? apiOrderStatus) =>
        apiCallSucceeded
        && apiOrderStatus is not null
        && (apiOrderStatus.Contains("cancel", StringComparison.OrdinalIgnoreCase)
            || apiOrderStatus.Contains("declined", StringComparison.OrdinalIgnoreCase)
            || apiOrderStatus.Contains("expired", StringComparison.OrdinalIgnoreCase));

    // ---------------------------------------------------------------- Geidea

    /// <summary>
    /// True only when Geidea answered and reports a definite failure. Anything else
    /// (no answer, still in progress, unknown status) leaves the order pending so the
    /// background reconciliation can settle it, instead of letting an unauthenticated
    /// callback cancel an order while the customer is still paying.
    /// </summary>
    public static bool GeideaIndicatesDefiniteFailure(GeideaPaymentStatusResponse result)
    {
        if (!result.Success || result.IsPaid || string.IsNullOrWhiteSpace(result.Status))
            return false;

        var status = result.Status;
        return status.Contains("fail", StringComparison.OrdinalIgnoreCase)
            || status.Contains("declin", StringComparison.OrdinalIgnoreCase)
            || status.Contains("reject", StringComparison.OrdinalIgnoreCase)
            || status.Contains("cancel", StringComparison.OrdinalIgnoreCase)
            || status.Contains("expire", StringComparison.OrdinalIgnoreCase)
            || status.Contains("void", StringComparison.OrdinalIgnoreCase);
    }
}
