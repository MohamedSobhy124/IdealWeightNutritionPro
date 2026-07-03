using IdealWeightNutrition.Contracts.Cart;
using IdealWeightNutrition.Contracts.Checkout;
using IdealWeightNutrition.Contracts.Services;
using IdealWeightNutrition.Domain.Checkout;
using IdealWeightNutrition.Domain.Services;

namespace IdealWeightNutrition.Application.Abstractions;

public interface IPaymentService
{
    Task<PaymentMethodsResponse> GetAvailableMethodsAsync(
        double orderTotal,
        CancellationToken cancellationToken = default);

    Task<PaymentInitiationResult> InitiateAsync(
        OrderHeader order,
        IReadOnlyList<CartItemDto> cartItems,
        CreateOrderRequest request,
        CancellationToken cancellationToken = default);

    Task<CompletePaymentResponse> CompleteAsync(
        int orderId,
        string? userId,
        string? guestCartId,
        CancellationToken cancellationToken = default);

    Task<CompletePaymentResponse> HandleTamaraReturnAsync(
        int orderId,
        string status,
        string? tamaraOrderId,
        string? userId,
        string? guestCartId,
        CancellationToken cancellationToken = default);

    Task<CompletePaymentResponse> HandleTabbyReturnAsync(
        int orderId,
        string status,
        string? paymentId,
        string? userId,
        string? guestCartId,
        CancellationToken cancellationToken = default);

    Task HandleGeideaCallbackAsync(int orderId, CancellationToken cancellationToken = default);

    Task<TamaraWebhookAckResponse> HandleTamaraWebhookAsync(
        TamaraNotificationPayload notification,
        string? authorizationHeader,
        CancellationToken cancellationToken = default);

    Task<PaymentInitiationResult> InitiateServicePurchaseAsync(
        ServicePurchase purchase,
        ServiceSubscription service,
        CreateServicePurchaseRequest request,
        CancellationToken cancellationToken = default);

    Task<CompleteServicePaymentResponse> CompleteServicePurchaseAsync(
        int purchaseId,
        CancellationToken cancellationToken = default);

    Task HandleServiceGeideaCallbackAsync(int purchaseId, CancellationToken cancellationToken = default);

    Task<CompleteServicePaymentResponse> HandleServiceTamaraReturnAsync(
        int purchaseId,
        string status,
        string? tamaraOrderId,
        CancellationToken cancellationToken = default);

    Task<CompleteServicePaymentResponse> HandleServiceTabbyReturnAsync(
        int purchaseId,
        string status,
        string? paymentId,
        CancellationToken cancellationToken = default);

    Task VerifyPendingPaymentsAsync(CancellationToken cancellationToken = default);
}

public sealed class PaymentInitiationResult
{
    public string? SessionId { get; init; }
    public string? RedirectUrl { get; init; }
}
