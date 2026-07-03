namespace IdealWeightNutrition.Application.Abstractions;

public interface IOtpService
{
    string GenerateOtp();
    Task StoreOtpAsync(string email, string otp, OtpPurpose purpose = OtpPurpose.Checkout, CancellationToken cancellationToken = default);
    Task<OtpVerificationResult> VerifyOtpAsync(string email, string otp, OtpPurpose purpose = OtpPurpose.Checkout, CancellationToken cancellationToken = default);
    Task<bool> IsEmailVerifiedAsync(string email, OtpPurpose purpose = OtpPurpose.Checkout, CancellationToken cancellationToken = default);
}

public sealed class OtpVerificationResult
{
    public bool IsValid { get; init; }
    public string? Message { get; init; }
}

public interface ICheckoutService
{
    Task<IReadOnlyList<Contracts.Checkout.CityDto>> ListCitiesAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Contracts.Checkout.RemoteAreaDto>> ListRemoteAreasAsync(int cityId, CancellationToken cancellationToken = default);
    Task<Contracts.Checkout.ShippingQuoteResponse> GetShippingQuoteAsync(
        string? userId,
        string? guestCartId,
        Contracts.Checkout.ShippingQuoteRequest request,
        CancellationToken cancellationToken = default);
    Task RequestCheckoutOtpAsync(string email, CancellationToken cancellationToken = default);
    Task<OtpVerificationResult> VerifyCheckoutOtpAsync(string email, string otp, CancellationToken cancellationToken = default);
    Task<Contracts.Checkout.PaymentMethodsResponse> GetPaymentMethodsAsync(
        double orderTotal,
        CancellationToken cancellationToken = default);
    Task<Contracts.Checkout.CompletePaymentResponse> CompletePaymentAsync(
        int orderId,
        string? userId,
        string? guestCartId,
        CancellationToken cancellationToken = default);
    Task<Contracts.Checkout.CreateOrderResponse> CreateOrderAsync(
        string? userId,
        string? guestCartId,
        Contracts.Checkout.CreateOrderRequest request,
        CancellationToken cancellationToken = default);
}
