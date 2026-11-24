using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using IdealWeightNutrition.Infrastructure.Payments;
using IdealWeightNutrition.Utility;
using Microsoft.IdentityModel.Tokens;

namespace IdealWeightNutrition.Application.Tests.Payments;

public class TabbyVerdictTests
{
    private static TappyPaymentStatusResponse Paid(string? reference = "42", decimal? amount = 100m, string status = "AUTHORIZED") =>
        new() { Success = true, IsPaid = true, Status = status, OrderReferenceId = reference, Amount = amount, TransactionId = "pay_1" };

    [Fact]
    public void Paid_payment_for_this_order_with_matching_amount_is_accepted()
    {
        var (isPaid, _) = PaymentVerificationRules.CheckTabbyVerdict(Paid(), 100m, "42");
        Assert.True(isPaid);
    }

    [Fact]
    public void Payment_created_for_another_order_is_rejected()
    {
        var (isPaid, message) = PaymentVerificationRules.CheckTabbyVerdict(Paid(reference: "7"), 100m, "42");
        Assert.False(isPaid);
        Assert.Contains("different order", message);
    }

    [Fact]
    public void Amount_mismatch_is_rejected()
    {
        var (isPaid, _) = PaymentVerificationRules.CheckTabbyVerdict(Paid(amount: 1m), 100m, "42");
        Assert.False(isPaid);
    }

    [Fact]
    public void Amount_within_tolerance_is_accepted()
    {
        var (isPaid, _) = PaymentVerificationRules.CheckTabbyVerdict(Paid(amount: 100.01m), 100m, "42");
        Assert.True(isPaid);
    }

    [Fact]
    public void Unpaid_or_failed_lookup_is_never_paid()
    {
        Assert.False(PaymentVerificationRules.CheckTabbyVerdict(new() { Success = true, IsPaid = false, Status = "CREATED" }, 100m, "42").IsPaid);
        Assert.False(PaymentVerificationRules.CheckTabbyVerdict(new() { Success = false, IsPaid = true }, 100m, "42").IsPaid);
    }

    [Fact]
    public void Payment_id_from_the_return_url_must_carry_this_orders_reference()
    {
        Assert.False(PaymentVerificationRules.TabbyPaymentBelongsTo(Paid(reference: null), "42", requireReference: true));
        Assert.True(PaymentVerificationRules.TabbyPaymentBelongsTo(Paid(reference: null), "42", requireReference: false));
        Assert.True(PaymentVerificationRules.TabbyPaymentBelongsTo(Paid(reference: "42"), "42", requireReference: true));
        Assert.False(PaymentVerificationRules.TabbyPaymentBelongsTo(Paid(reference: "7"), "42", requireReference: false));
    }
}

public class TamaraWebhookTokenTests
{
    private const string NotificationToken = "e7a1c2d4-0000-4b4b-9c9c-1234567890ab";

    private static string SignJwt(string key, DateTime? expires = null, DateTime? notBefore = null)
    {
        var handler = new JwtSecurityTokenHandler();
        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
            SecurityAlgorithms.HmacSha256);
        var token = handler.CreateJwtSecurityToken(
            issuer: "Tamara",
            subject: new ClaimsIdentity(),
            notBefore: notBefore ?? DateTime.UtcNow.AddMinutes(-1),
            expires: expires ?? DateTime.UtcNow.AddMinutes(10),
            issuedAt: notBefore ?? DateTime.UtcNow.AddMinutes(-1),
            signingCredentials: credentials);
        return handler.WriteToken(token);
    }

    [Fact]
    public void Jwt_signed_with_the_notification_token_is_valid()
    {
        Assert.True(PaymentVerificationRules.IsValidTamaraWebhookToken(SignJwt(NotificationToken), NotificationToken));
    }

    [Fact]
    public void Jwt_signed_with_another_key_is_rejected()
    {
        var forged = SignJwt("f0f0f0f0-1111-2222-3333-444444444444");
        Assert.False(PaymentVerificationRules.IsValidTamaraWebhookToken(forged, NotificationToken));
    }

    [Fact]
    public void Expired_jwt_is_rejected()
    {
        var expired = SignJwt(
            NotificationToken,
            expires: DateTime.UtcNow.AddHours(-1),
            notBefore: DateTime.UtcNow.AddHours(-2));
        Assert.False(PaymentVerificationRules.IsValidTamaraWebhookToken(expired, NotificationToken));
    }

    [Fact]
    public void Unsigned_jwt_is_rejected()
    {
        // header {"alg":"none","typ":"JWT"} . payload {"iss":"Tamara"} . (no signature)
        const string unsigned = "eyJhbGciOiJub25lIiwidHlwIjoiSldUIn0.eyJpc3MiOiJUYW1hcmEifQ.";
        Assert.False(PaymentVerificationRules.IsValidTamaraWebhookToken(unsigned, NotificationToken));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("garbage")]
    public void Missing_or_malformed_token_is_rejected(string? token)
    {
        Assert.False(PaymentVerificationRules.IsValidTamaraWebhookToken(token, NotificationToken));
    }

    [Fact]
    public void Everything_is_rejected_when_no_notification_token_is_configured()
    {
        Assert.False(PaymentVerificationRules.IsValidTamaraWebhookToken(SignJwt(NotificationToken), ""));
        Assert.False(PaymentVerificationRules.IsValidTamaraWebhookToken("", ""));
    }

    [Fact]
    public void Token_is_read_from_header_first_then_query()
    {
        Assert.Equal("abc", PaymentVerificationRules.ExtractTamaraToken("Bearer abc", "xyz"));
        Assert.Equal("xyz", PaymentVerificationRules.ExtractTamaraToken("", "xyz"));
        Assert.Equal("xyz", PaymentVerificationRules.ExtractTamaraToken("Basic abc", "xyz"));
        Assert.Null(PaymentVerificationRules.ExtractTamaraToken(null, null));
    }

    [Theory]
    [InlineData(true, "canceled", true)]
    [InlineData(true, "declined", true)]
    [InlineData(true, "expired", true)]
    [InlineData(true, "approved", false)]
    [InlineData(false, "canceled", false)]
    [InlineData(true, null, false)]
    public void Cancellation_requires_tamara_api_confirmation(bool apiOk, string? apiStatus, bool expected)
    {
        Assert.Equal(expected, PaymentVerificationRules.TamaraApiIndicatesCancelled(apiOk, apiStatus));
    }
}

public class GeideaCallbackRuleTests
{
    [Theory]
    [InlineData(true, false, "Failed", true)]
    [InlineData(true, false, "Declined", true)]
    [InlineData(true, false, "Cancelled", true)]
    [InlineData(true, false, "Expired", true)]
    [InlineData(true, false, "Initiated", false)]
    [InlineData(true, false, "InProgress", false)]
    [InlineData(true, false, null, false)]
    [InlineData(false, false, "Failed", false)]
    [InlineData(true, true, "Success", false)]
    public void Only_a_definite_failure_reported_by_geidea_cancels(bool success, bool isPaid, string? status, bool expected)
    {
        var result = new GeideaPaymentStatusResponse { Success = success, IsPaid = isPaid, Status = status! };
        Assert.Equal(expected, PaymentVerificationRules.GeideaIndicatesDefiniteFailure(result));
    }
}

public class TamaraReturnBindingTests
{
    [Theory]
    [InlineData(true, "42", "42", true)]
    [InlineData(true, " 42 ", "42", true)]
    [InlineData(true, "7", "42", false)]
    [InlineData(true, null, "42", false)]
    [InlineData(true, "", "42", false)]
    [InlineData(false, "42", "42", false)]
    public void Tamara_order_from_the_url_must_belong_to_this_order(bool apiOk, string? reference, string expected, bool result)
    {
        Assert.Equal(result, PaymentVerificationRules.TamaraOrderBelongsTo(apiOk, reference, expected));
    }
}
