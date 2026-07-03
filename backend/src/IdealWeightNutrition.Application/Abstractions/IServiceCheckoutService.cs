using IdealWeightNutrition.Contracts.Checkout;
using IdealWeightNutrition.Contracts.Services;

namespace IdealWeightNutrition.Application.Abstractions;

public interface IServiceCheckoutService
{
    Task<ServiceCheckoutQuoteResponse> GetQuoteAsync(
        ServiceCheckoutQuoteRequest request,
        CancellationToken cancellationToken = default);

    Task RequestCheckoutOtpAsync(string email, CancellationToken cancellationToken = default);

    Task<OtpVerificationResult> VerifyCheckoutOtpAsync(
        string email,
        string otp,
        CancellationToken cancellationToken = default);

    Task<PaymentMethodsResponse> GetPaymentMethodsAsync(
        double amountToPay,
        CancellationToken cancellationToken = default);

    Task<CreateServicePurchaseResponse> CreatePurchaseAsync(
        int serviceId,
        string? userId,
        CreateServicePurchaseRequest request,
        CancellationToken cancellationToken = default);

    Task<CompleteServicePaymentResponse> CompletePaymentAsync(
        int purchaseId,
        CancellationToken cancellationToken = default);
}
