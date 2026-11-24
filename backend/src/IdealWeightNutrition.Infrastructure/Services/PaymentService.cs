using System.Security.Cryptography;
using System.Text;
using IdealWeightNutrition.Application.Abstractions;
using IdealWeightNutrition.Contracts.Cart;
using IdealWeightNutrition.Contracts.Checkout;
using IdealWeightNutrition.Contracts.Services;
using IdealWeightNutrition.Domain.Checkout;
using IdealWeightNutrition.Domain.Constants;
using IdealWeightNutrition.Domain.Services;
using IdealWeightNutrition.Infrastructure.Options;
using IdealWeightNutrition.Infrastructure.Payments;
using IdealWeightNutrition.Infrastructure.Persistence;
using IdealWeightNutrition.Utility;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace IdealWeightNutrition.Infrastructure.Services;

internal sealed class PaymentService : IPaymentService
{
    private const string ServicePaymentApproved = "Approved";

    private readonly AppDbContext _db;
    private readonly ICartService _cart;
    private readonly IInventoryService _inventory;
    private readonly IPromoCodeService _promoCodes;
    private readonly IDateTimeProvider _clock;
    private readonly GeideaSettings _geidea;
    private readonly TamaraSettings _tamara;
    private readonly TappySettings _tabby;
    private readonly AppUrlOptions _urls;
    private readonly IOrderNotificationService _orderNotifications;
    private readonly PaymentVerificationOptions _paymentVerification;
    private readonly ILogger<PaymentService> _logger;

    public PaymentService(
        AppDbContext db,
        ICartService cart,
        IInventoryService inventory,
        IPromoCodeService promoCodes,
        IDateTimeProvider clock,
        IOptions<GeideaSettings> geidea,
        IOptions<TamaraSettings> tamara,
        IOptions<TappySettings> tabby,
        IOptions<AppUrlOptions> urls,
        IOptions<PaymentVerificationOptions> paymentVerification,
        IOrderNotificationService orderNotifications,
        ILogger<PaymentService> logger)
    {
        _db = db;
        _cart = cart;
        _inventory = inventory;
        _promoCodes = promoCodes;
        _clock = clock;
        _geidea = geidea.Value;
        _tamara = tamara.Value;
        _tabby = tabby.Value;
        _urls = urls.Value;
        _paymentVerification = paymentVerification.Value;
        _orderNotifications = orderNotifications;
        _logger = logger;
    }

    public async Task<PaymentMethodsResponse> GetAvailableMethodsAsync(
        double orderTotal,
        CancellationToken cancellationToken = default)
    {
        var methods = new List<PaymentMethodOptionDto>
        {
            new()
            {
                Id = PaymentMethods.Cod,
                Label = "Cash on delivery",
                Available = true
            }
        };

        var geideaConfigured = !string.IsNullOrWhiteSpace(_geidea.MerchantPublicKey)
            && !string.IsNullOrWhiteSpace(_geidea.MerchantApiPassword);
        methods.Add(new PaymentMethodOptionDto
        {
            Id = PaymentMethods.Geidea,
            Label = "Card (Geidea)",
            Available = geideaConfigured,
            UnavailableReasonCode = geideaConfigured ? null : PaymentUnavailableReasons.NotConfigured,
            UnavailableReason = geideaConfigured ? null : "Card payments are not configured."
        });

        var tamaraAvailable = false;
        string? tamaraReason = null;
        string? tamaraReasonCode = null;
        var totalAmount = (decimal)orderTotal;
        if (!_tamara.Enabled)
        {
            tamaraReasonCode = PaymentUnavailableReasons.Disabled;
            tamaraReason = "Tamara is disabled.";
        }
        else if (totalAmount < _tamara.MinimumOrderAmount)
        {
            tamaraReasonCode = PaymentUnavailableReasons.MinAmount;
            tamaraReason = $"Minimum order amount is AED {_tamara.MinimumOrderAmount:0}.";
        }
        else if (string.IsNullOrWhiteSpace(_tamara.ApiToken))
        {
            tamaraReasonCode = PaymentUnavailableReasons.NotConfigured;
            tamaraReason = "Tamara is not configured.";
        }
        else
        {
            try
            {
                var helper = new TamaraHelper(_tamara);
                tamaraAvailable = await helper.IsPaymentAvailableAsync(totalAmount, _tamara.CountryCode ?? "AE");
                if (!tamaraAvailable)
                {
                    tamaraReasonCode = PaymentUnavailableReasons.AmountNotEligible;
                    tamaraReason = "Tamara is not available for this order amount.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Tamara availability check failed");
                tamaraReasonCode = PaymentUnavailableReasons.TemporarilyUnavailable;
                tamaraReason = "Tamara is temporarily unavailable.";
            }
        }

        methods.Add(new PaymentMethodOptionDto
        {
            Id = PaymentMethods.Tamara,
            Label = "Tamara — Buy now, pay later",
            Available = tamaraAvailable,
            UnavailableReason = tamaraAvailable ? null : tamaraReason,
            UnavailableReasonCode = tamaraAvailable ? null : tamaraReasonCode,
            MinimumAmount = (double)_tamara.MinimumOrderAmount
        });

        var tabbyAvailable = _tabby.Enabled
            && !string.IsNullOrWhiteSpace(_tabby.ApiKey)
            && totalAmount >= _tabby.MinimumOrderAmount;
        string? tabbyReasonCode = null;
        string? tabbyReason = null;
        if (!tabbyAvailable)
        {
            if (!_tabby.Enabled)
            {
                tabbyReasonCode = PaymentUnavailableReasons.Disabled;
                tabbyReason = "Tabby is disabled.";
            }
            else if (totalAmount < _tabby.MinimumOrderAmount)
            {
                tabbyReasonCode = PaymentUnavailableReasons.MinAmount;
                tabbyReason = $"Minimum order amount is AED {_tabby.MinimumOrderAmount:0}.";
            }
            else
            {
                tabbyReasonCode = PaymentUnavailableReasons.NotConfigured;
                tabbyReason = "Tabby is not configured.";
            }
        }

        methods.Add(new PaymentMethodOptionDto
        {
            Id = PaymentMethods.Tabby,
            Label = "Tabby — Pay in 4",
            Available = tabbyAvailable,
            UnavailableReason = tabbyReason,
            UnavailableReasonCode = tabbyReasonCode,
            MinimumAmount = (double)_tabby.MinimumOrderAmount
        });

        return new PaymentMethodsResponse { Methods = methods };
    }

    public async Task<PaymentInitiationResult> InitiateAsync(
        OrderHeader order,
        IReadOnlyList<CartItemDto> cartItems,
        CreateOrderRequest request,
        CancellationToken cancellationToken = default)
    {
        return order.PaymentMethod switch
        {
            PaymentMethods.Geidea => await InitiateGeideaAsync(order, request, cancellationToken),
            PaymentMethods.Tamara => await InitiateTamaraAsync(order, cartItems, request, cancellationToken),
            PaymentMethods.Tabby => await InitiateTabbyAsync(order, cartItems, request, cancellationToken),
            _ => throw new InvalidOperationException("Unsupported online payment method.")
        };
    }

    public async Task<CompletePaymentResponse> CompleteAsync(
        int orderId,
        string? userId,
        string? guestCartId,
        CancellationToken cancellationToken = default)
    {
        var order = await _db.OrderHeaders.FirstOrDefaultAsync(o => o.Id == orderId, cancellationToken)
            ?? throw new InvalidOperationException("Order not found.");

        if (IsPaid(order))
        {
            return new CompletePaymentResponse
            {
                OrderId = order.Id,
                OrderStatus = order.OrderStatus ?? OrderStatuses.Pending,
                PaymentStatus = order.PaymentStatus ?? PaymentStatuses.Pending,
                IsPaid = true,
                Message = "Payment already completed."
            };
        }

        var verified = order.PaymentMethod switch
        {
            PaymentMethods.Geidea => await VerifyGeideaAsync(order, cancellationToken),
            PaymentMethods.Tamara => await VerifyTamaraAsync(order, null, cancellationToken),
            PaymentMethods.Tabby => await VerifyTabbyAsync(order, cancellationToken),
            _ => throw new InvalidOperationException("Order does not require online payment completion.")
        };

        if (!verified.IsPaid)
        {
            return new CompletePaymentResponse
            {
                OrderId = order.Id,
                OrderStatus = order.OrderStatus ?? OrderStatuses.Pending,
                PaymentStatus = order.PaymentStatus ?? PaymentStatuses.Pending,
                IsPaid = false,
                Message = verified.Message
            };
        }

        await MarkPaidAndFulfillAsync(order, userId, guestCartId, cancellationToken);

        return new CompletePaymentResponse
        {
            OrderId = order.Id,
            OrderStatus = order.OrderStatus ?? OrderStatuses.Paid,
            PaymentStatus = order.PaymentStatus ?? PaymentStatuses.Paid,
            IsPaid = true,
            Message = verified.Message
        };
    }

    public async Task<CompletePaymentResponse> HandleTamaraReturnAsync(
        int orderId,
        string status,
        string? tamaraOrderId,
        string? userId,
        string? guestCartId,
        CancellationToken cancellationToken = default)
    {
        if (!status.Equals("success", StringComparison.OrdinalIgnoreCase))
        {
            // The URL is public, so never trust its status on its own: only close the
            // order now when Tamara itself reports it declined, cancelled or expired.
            var failedOrder = await _db.OrderHeaders.FirstOrDefaultAsync(o => o.Id == orderId, cancellationToken);
            if (failedOrder is { PaymentMethod: PaymentMethods.Tamara }
                && !IsPaid(failedOrder)
                && !string.IsNullOrEmpty(failedOrder.PaymentIntentId ?? tamaraOrderId))
            {
                try
                {
                    var state = await new TamaraHelper(_tamara)
                        .GetOrderDetailsAsync(failedOrder.PaymentIntentId ?? tamaraOrderId!);
                    var reported = state.Status ?? string.Empty;
                    if (state.Success
                        && (reported.Contains("declin", StringComparison.OrdinalIgnoreCase)
                            || reported.Contains("cancel", StringComparison.OrdinalIgnoreCase)
                            || reported.Contains("expire", StringComparison.OrdinalIgnoreCase)))
                    {
                        await CancelUnpaidOrderAsync(
                            failedOrder,
                            cancellationToken,
                            $"Tamara reports the payment as \"{reported}\".");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Could not verify failed Tamara return for order #{OrderId}", orderId);
                }
            }

            return new CompletePaymentResponse
            {
                OrderId = orderId,
                OrderStatus = failedOrder?.OrderStatus ?? OrderStatuses.Pending,
                PaymentStatus = failedOrder?.PaymentStatus ?? PaymentStatuses.Pending,
                IsPaid = false,
                Message = "Payment was not completed."
            };
        }

        return await CompleteTamaraReturnAsync(orderId, tamaraOrderId, userId, guestCartId, cancellationToken);
    }

    public Task<CompletePaymentResponse> HandleTabbyReturnAsync(
        int orderId,
        string status,
        string? paymentId,
        string? userId,
        string? guestCartId,
        CancellationToken cancellationToken = default) =>
        CompleteTabbyReturnAsync(orderId, status, paymentId, userId, guestCartId, cancellationToken);

    public async Task HandleGeideaCallbackAsync(int orderId, CancellationToken cancellationToken = default)
    {
        var order = await _db.OrderHeaders.FirstOrDefaultAsync(o => o.Id == orderId, cancellationToken);
        if (order is null || IsPaid(order))
            return;

        // The callback itself is unauthenticated (only an order id in the query), so it is
        // treated purely as a "go check now" signal: Geidea is asked server to server, and the
        // order is cancelled only when Geidea reports a definite failure. Anything else is left
        // pending for the background reconciliation, so a forged callback can neither mark an
        // order paid nor cancel one while the customer is still paying.
        var result = await new GeideaHelper(_geidea).VerifyPaymentAsync(order.Id.ToString());

        if (result is { Success: true, IsPaid: true })
        {
            var amountCheck = ValidateGeideaPaidAmount((decimal)order.OrderTotal, result.PaidAmount, order.Id, "order");
            if (!amountCheck.Ok)
                return;

            await MarkPaidAndFulfillAsync(order, order.ApplicationUserId, null, cancellationToken);
            return;
        }

        if (PaymentVerificationRules.GeideaIndicatesDefiniteFailure(result))
        {
            await CancelUnpaidOrderAsync(
                order,
                cancellationToken,
                $"Geidea callback: payment was not completed. {result.Message}");
            return;
        }

        _logger.LogInformation(
            "Geidea callback for order {OrderId}: not paid yet ({Status}); left pending for reconciliation.",
            order.Id,
            result.Status);
    }

    public async Task<TamaraWebhookAckResponse> HandleTamaraWebhookAsync(
        TamaraNotificationPayload notification,
        string? authorizationHeader,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(notification.OrderId))
        {
            _logger.LogWarning("Tamara webhook received without order_id.");
            return new TamaraWebhookAckResponse
            {
                Success = false,
                Message = "Invalid payload — order_id is required."
            };
        }

        if (!ValidateTamaraWebhookAuthorization(authorizationHeader))
        {
            _logger.LogWarning("Tamara webhook rejected due to missing or invalid Authorization header.");
            return new TamaraWebhookAckResponse
            {
                Success = false,
                Message = "Invalid authorization header.",
                TamaraOrderId = notification.OrderId
            };
        }

        _logger.LogInformation(
            "Tamara webhook: OrderId={OrderId}, ReferenceId={ReferenceId}, OrderStatus={OrderStatus}, PaymentStatus={PaymentStatus}",
            notification.OrderId,
            notification.OrderReferenceId,
            notification.OrderStatus,
            notification.PaymentStatus);

        var order = await _db.OrderHeaders
            .FirstOrDefaultAsync(o => o.PaymentIntentId == notification.OrderId, cancellationToken);

        ServicePurchase? purchase = null;
        if (order is null)
        {
            purchase = await _db.ServicePurchases
                .FirstOrDefaultAsync(p => p.SessionId == notification.OrderId, cancellationToken);
        }

        if (order is null && purchase is null && int.TryParse(notification.OrderReferenceId, out var referenceId))
        {
            order = await _db.OrderHeaders.FirstOrDefaultAsync(o => o.Id == referenceId, cancellationToken);
            if (order is null)
            {
                purchase = await _db.ServicePurchases
                    .FirstOrDefaultAsync(
                        p => p.Id == referenceId && p.PaymentIntentId == PaymentMethods.Tamara,
                        cancellationToken);
            }
        }

        if (order is null && purchase is null)
        {
            _logger.LogWarning("Tamara webhook: no local order or service purchase for Tamara order {OrderId}.", notification.OrderId);
            return new TamaraWebhookAckResponse
            {
                Success = false,
                Message = "Order not found.",
                TamaraOrderId = notification.OrderId
            };
        }

        var helper = new TamaraHelper(_tamara);
        var orderDetails = await helper.GetOrderDetailsAsync(notification.OrderId);
        if (orderDetails.Success)
        {
            _logger.LogInformation(
                "Tamara API status for {OrderId}: status={Status}, paymentStatus={PaymentStatus}",
                notification.OrderId,
                orderDetails.Status,
                orderDetails.PaymentStatus);
        }

        var isApproved = TamaraStatusIndicatesApproved(
            notification.OrderStatus,
            notification.PaymentStatus,
            orderDetails.Status,
            orderDetails.PaymentStatus);

        if (isApproved)
        {
            if (order is not null)
                return await ProcessTamaraWebhookForProductOrderAsync(order, notification.OrderId, cancellationToken);

            return await ProcessTamaraWebhookForServicePurchaseAsync(purchase!, notification.OrderId, cancellationToken);
        }

        // Cancel only when Tamara's API confirms it; the webhook body alone is not trusted.
        if (PaymentVerificationRules.TamaraApiIndicatesCancelled(orderDetails.Success, orderDetails.Status))
        {
            if (order is not null)
            {
                await CancelUnpaidOrderAsync(
                    order,
                    cancellationToken,
                    $"Tamara reports the order as \"{orderDetails.Status}\" (webhook, confirmed with the Tamara API).");

                return new TamaraWebhookAckResponse
                {
                    Success = true,
                    Message = "Order cancelled.",
                    TamaraOrderId = notification.OrderId
                };
            }

            if (!IsServicePaid(purchase!) && purchase!.Status != "Cancelled")
            {
                purchase.Status = "Cancelled";
                purchase.PaymentStatus = PaymentStatuses.Rejected;
                await _db.SaveChangesAsync(cancellationToken);
                _logger.LogInformation("Tamara webhook marked service purchase {PurchaseId} as cancelled.", purchase.Id);
            }

            return new TamaraWebhookAckResponse
            {
                Success = true,
                Message = "Service purchase cancelled.",
                TamaraOrderId = notification.OrderId
            };
        }

        return new TamaraWebhookAckResponse
        {
            Success = true,
            Message = "Notification acknowledged.",
            TamaraOrderId = notification.OrderId
        };
    }

    private async Task<TamaraWebhookAckResponse> ProcessTamaraWebhookForProductOrderAsync(
        OrderHeader order,
        string tamaraOrderId,
        CancellationToken cancellationToken)
    {
        if (order.PaymentIntentId != tamaraOrderId)
        {
            order.PaymentIntentId = tamaraOrderId;
            await _db.SaveChangesAsync(cancellationToken);
        }

        var verified = await VerifyTamaraAsync(order, tamaraOrderId, cancellationToken);
        if (!verified.IsPaid)
        {
            _logger.LogWarning(
                "Tamara webhook authorization/verification failed for product order {OrderId}: {Message}",
                order.Id,
                verified.Message);

            return new TamaraWebhookAckResponse
            {
                Success = false,
                Message = verified.Message ?? "Tamara payment verification failed.",
                TamaraOrderId = tamaraOrderId
            };
        }

        await MarkPaidAndFulfillAsync(order, order.ApplicationUserId, null, cancellationToken);
        _logger.LogInformation("Tamara webhook marked product order {OrderId} as paid.", order.Id);

        return new TamaraWebhookAckResponse
        {
            Success = true,
            Message = "Order authorized and marked paid.",
            TamaraOrderId = tamaraOrderId
        };
    }

    private async Task<TamaraWebhookAckResponse> ProcessTamaraWebhookForServicePurchaseAsync(
        ServicePurchase purchase,
        string tamaraOrderId,
        CancellationToken cancellationToken)
    {
        if (purchase.SessionId != tamaraOrderId)
        {
            purchase.SessionId = tamaraOrderId;
            await _db.SaveChangesAsync(cancellationToken);
        }

        var verified = await VerifyServiceTamaraAsync(purchase, tamaraOrderId, cancellationToken);
        if (!verified.IsPaid)
        {
            _logger.LogWarning(
                "Tamara webhook authorization/verification failed for service purchase {PurchaseId}: {Message}",
                purchase.Id,
                verified.Message);

            return new TamaraWebhookAckResponse
            {
                Success = false,
                Message = verified.Message ?? "Tamara payment verification failed.",
                TamaraOrderId = tamaraOrderId
            };
        }

        await MarkServicePurchasePaidAsync(purchase, cancellationToken);
        _logger.LogInformation("Tamara webhook marked service purchase {PurchaseId} as paid.", purchase.Id);

        return new TamaraWebhookAckResponse
        {
            Success = true,
            Message = "Service purchase authorized and marked paid.",
            TamaraOrderId = tamaraOrderId
        };
    }

    private bool ValidateTamaraWebhookAuthorization(string? authorizationHeader)
    {
        if (string.IsNullOrWhiteSpace(_tamara.NotificationToken))
        {
            _logger.LogError("Tamara webhook rejected: Tamara:NotificationToken is not configured.");
            return false;
        }

        var token = PaymentVerificationRules.ExtractTamaraToken(authorizationHeader, queryToken: null);
        return PaymentVerificationRules.IsValidTamaraWebhookToken(token, _tamara.NotificationToken);
    }

    private static bool AmountsMatch(decimal expected, decimal actual) =>
        PaymentVerificationRules.AmountsMatch(expected, actual);

    private (bool Ok, string? Error) ValidateGeideaPaidAmount(decimal expected, decimal? paidAmount, int entityId, string entityType)
    {
        if (!paidAmount.HasValue)
            return (true, null);

        if (AmountsMatch(expected, paidAmount.Value))
            return (true, null);

        _logger.LogWarning(
            "Geidea amount mismatch for {EntityType} {EntityId}: expected {Expected}, got {Actual}",
            entityType,
            entityId,
            expected,
            paidAmount.Value);

        return (false, "Payment amount does not match the order total.");
    }

    private (bool Ok, string? Error) ValidateTamaraPaidAmount(decimal expected, decimal? paidAmount, int entityId, string entityType)
    {
        if (!paidAmount.HasValue)
            return (true, null);

        if (AmountsMatch(expected, paidAmount.Value))
            return (true, null);

        _logger.LogWarning(
            "Tamara amount mismatch for {EntityType} {EntityId}: expected {Expected}, got {Actual}",
            entityType,
            entityId,
            expected,
            paidAmount.Value);

        return (false, "Tamara payment amount does not match the order total.");
    }

    private static bool TamaraStatusIndicatesApproved(
        string? notificationOrderStatus,
        string? notificationPaymentStatus,
        string? apiOrderStatus,
        string? apiPaymentStatus) =>
        ContainsTamaraKeyword(notificationOrderStatus, "approved")
        || ContainsTamaraKeyword(notificationPaymentStatus, "approved")
        || ContainsTamaraKeyword(notificationOrderStatus, "authorised")
        || ContainsTamaraKeyword(notificationPaymentStatus, "authorised")
        || ContainsTamaraKeyword(apiOrderStatus, "approved")
        || ContainsTamaraKeyword(apiPaymentStatus, "approved")
        || ContainsTamaraKeyword(apiOrderStatus, "authorised")
        || ContainsTamaraKeyword(apiPaymentStatus, "authorised");

    private static bool ContainsTamaraKeyword(string? value, string keyword) =>
        value?.Contains(keyword, StringComparison.OrdinalIgnoreCase) == true;

    private async Task<PaymentInitiationResult> InitiateGeideaAsync(
        OrderHeader order,
        CreateOrderRequest request,
        CancellationToken cancellationToken)
    {
        var apiBase = _urls.PublicApiBaseUrl.TrimEnd('/');
        var callbackBase = string.IsNullOrWhiteSpace(_geidea.CallbackUrlOverride)
            ? apiBase
            : _geidea.CallbackUrlOverride.TrimEnd('/');

        var callbackUrl = $"{callbackBase}/api/payments/geidea/callback?orderId={order.Id}";
        var returnUrl = $"{_urls.FrontendBaseUrl.TrimEnd('/')}/order/confirmation/{order.Id}";

        if ((callbackUrl.Contains("localhost", StringComparison.OrdinalIgnoreCase)
                || callbackUrl.Contains("127.0.0.1", StringComparison.OrdinalIgnoreCase)
                || !callbackUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            && string.IsNullOrWhiteSpace(_geidea.CallbackUrlOverride))
        {
            throw new InvalidOperationException(
                "Geidea requires a public HTTPS callback URL. Set Geidea:CallbackUrlOverride in appsettings (e.g. ngrok URL) for local testing.");
        }

        var helper = new GeideaHelper(_geidea);
        var response = await helper.CreatePaymentAsync(new GeideaPaymentRequest
        {
            Amount = (decimal)order.OrderTotal,
            Currency = "AED",
            OrderId = order.Id.ToString(),
            CustomerName = order.Name,
            CustomerEmail = order.Email ?? request.Email,
            CustomerPhone = order.PhoneNumber,
            ReturnUrl = callbackUrl,
            CancelUrl = $"{_urls.FrontendBaseUrl.TrimEnd('/')}/checkout",
            BillingAddress = order.StreetAddress,
            BillingCity = order.City,
            BillingState = order.State,
            BillingPostalCode = order.PostalCode,
            BillingCountryCode = "AE"
        });

        if (!response.Success || string.IsNullOrEmpty(response.TransactionId))
            throw new InvalidOperationException(response.Message ?? "Failed to create Geidea payment session.");

        order.SessionId = response.TransactionId;
        order.PaymentIntentId = response.TransactionId;
        await _db.SaveChangesAsync(cancellationToken);

        return new PaymentInitiationResult
        {
            SessionId = response.TransactionId,
            RedirectUrl = string.IsNullOrEmpty(response.PaymentUrl) ? null : response.PaymentUrl
        };
    }

    private async Task<PaymentInitiationResult> InitiateTamaraAsync(
        OrderHeader order,
        IReadOnlyList<CartItemDto> cartItems,
        CreateOrderRequest request,
        CancellationToken cancellationToken)
    {
        if (!_tamara.Enabled)
            throw new InvalidOperationException("Tamara payment is not available.");

        var apiBase = _urls.PublicApiBaseUrl.TrimEnd('/');
        var currency = _tamara.Currency ?? "AED";
        var countryCode = _tamara.CountryCode ?? "AE";
        var nameParts = order.Name.Trim().Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
        var firstName = nameParts[0];
        var lastName = nameParts.Length > 1 ? nameParts[1] : firstName;
        var phone = FormatTamaraPhone(order.PhoneNumber);
        var postalCode = string.IsNullOrWhiteSpace(order.PostalCode) ? "00000" : order.PostalCode;

        var helper = new TamaraHelper(_tamara);
        var tamaraRequest = new TamaraPaymentRequest
        {
            OrderReferenceId = order.Id.ToString(),
            OrderNumber = $"ORD-{order.Id}",
            TotalAmount = new TamaraAmount { Amount = (decimal)order.OrderTotal, Currency = currency },
            Description = $"Order {order.Id}",
            CountryCode = countryCode,
            PaymentType = "PAY_BY_INSTALMENTS",
            Locale = "en_AE",
            Platform = "IdealWeightNutrition.Api",
            MerchantUrl = new TamaraMerchantUrl
            {
                Success = $"{apiBase}/api/payments/tamara/return?orderId={order.Id}&status=success",
                Failure = $"{apiBase}/api/payments/tamara/return?orderId={order.Id}&status=failure",
                Cancel = $"{_urls.FrontendBaseUrl.TrimEnd('/')}/checkout",
                Notification = $"{apiBase}/api/payments/tamara/webhook"
            },
            Consumer = new TamaraConsumer
            {
                FirstName = firstName,
                LastName = lastName,
                PhoneNumber = phone,
                Email = order.Email ?? request.Email
            },
            BillingAddress = BuildTamaraAddress(firstName, lastName, order, phone, countryCode, postalCode),
            ShippingAddress = BuildTamaraAddress(firstName, lastName, order, phone, countryCode, postalCode),
            Items = cartItems.Select(item => new TamaraItem
            {
                ReferenceId = item.ProductId.ToString(),
                Type = "Physical",
                Name = item.Title.Length > 200 ? item.Title[..200] : item.Title,
                Sku = item.ProductId.ToString(),
                Quantity = item.Quantity,
                UnitPrice = new TamaraAmount { Amount = (decimal)item.UnitPrice, Currency = currency },
                TotalAmount = new TamaraAmount { Amount = (decimal)(item.UnitPrice * item.Quantity), Currency = currency },
                DiscountAmount = new TamaraAmount { Amount = 0, Currency = currency },
                TaxAmount = new TamaraAmount { Amount = 0, Currency = currency }
            }).ToList(),
            TaxAmount = new TamaraAmount { Amount = 0, Currency = currency },
            ShippingAmount = new TamaraAmount { Amount = 0, Currency = currency }
        };

        if (order.DiscountAmount is > 0)
        {
            tamaraRequest.Discount = new TamaraDiscount
            {
                Name = "Promo discount",
                Amount = new TamaraAmount { Amount = (decimal)order.DiscountAmount.Value, Currency = currency }
            };
        }

        var response = await helper.CreateCheckoutAsync(tamaraRequest);
        if (!response.Success || string.IsNullOrEmpty(response.CheckoutUrl))
            throw new InvalidOperationException(response.Message ?? "Failed to create Tamara checkout.");

        order.SessionId = response.CheckoutId;
        order.PaymentIntentId = response.OrderId;
        await _db.SaveChangesAsync(cancellationToken);

        return new PaymentInitiationResult { RedirectUrl = response.CheckoutUrl };
    }

    private async Task<PaymentInitiationResult> InitiateTabbyAsync(
        OrderHeader order,
        IReadOnlyList<CartItemDto> cartItems,
        CreateOrderRequest request,
        CancellationToken cancellationToken)
    {
        if (!_tabby.Enabled)
            throw new InvalidOperationException("Tabby payment is not available.");

        var apiBase = _urls.PublicApiBaseUrl.TrimEnd('/');
        var frontend = _urls.FrontendBaseUrl.TrimEnd('/');
        var helper = new TappyHelper(_tabby);

        var tabbyItems = cartItems.Select(item =>
        {
            var imageUrl = string.IsNullOrWhiteSpace(item.ImageUrl)
                ? $"{frontend}/favicon.ico"
                : item.ImageUrl.StartsWith("http", StringComparison.OrdinalIgnoreCase)
                    ? item.ImageUrl
                    : $"{frontend}/{item.ImageUrl.TrimStart('/')}";

            return new TabbyOrderItem
            {
                ReferenceId = item.ProductId.ToString(),
                Title = item.Title,
                Description = item.Title,
                Quantity = item.Quantity,
                UnitPrice = (decimal)item.UnitPrice,
                DiscountAmount = 0,
                ImageUrl = imageUrl,
                ProductUrl = $"{frontend}/product/{item.Slug}",
                Category = "General"
            };
        }).ToList();

        var response = await helper.CreatePaymentAsync(new TappyPaymentRequest
        {
            MerchantId = _tabby.MerchantId,
            Amount = (decimal)order.OrderTotal,
            Currency = "AED",
            OrderId = order.Id.ToString(),
            CustomerName = order.Name,
            CustomerEmail = order.Email ?? request.Email,
            CustomerPhone = order.PhoneNumber,
            ReturnUrl = $"{apiBase}/api/payments/tabby/return?orderId={order.Id}",
            CancelUrl = $"{frontend}/checkout",
            Description = $"Order #{order.Id}",
            ShippingCity = order.City,
            ShippingAddress = order.StreetAddress,
            ShippingPostalCode = order.PostalCode,
            DiscountAmount = order.DiscountAmount is > 0 ? (decimal)order.DiscountAmount.Value : null,
            Language = "en",
            Items = tabbyItems
        });

        if (!response.Success || string.IsNullOrEmpty(response.PaymentUrl))
            throw new InvalidOperationException(response.Message ?? "Failed to create Tabby payment.");

        order.SessionId = response.TransactionId;
        await _db.SaveChangesAsync(cancellationToken);

        return new PaymentInitiationResult { RedirectUrl = response.PaymentUrl };
    }

    private async Task<CompletePaymentResponse> CompleteTamaraReturnAsync(
        int orderId,
        string? tamaraOrderId,
        string? userId,
        string? guestCartId,
        CancellationToken cancellationToken)
    {
        var order = await _db.OrderHeaders.FirstOrDefaultAsync(o => o.Id == orderId, cancellationToken)
            ?? throw new InvalidOperationException("Order not found.");

        // The Tamara order id in the return URL can be edited by the customer. The id we
        // stored when the checkout was created wins; a URL id is only used when we have none,
        // and then only if Tamara confirms it was created for this order.
        string? effectiveTamaraOrderId = null;
        var fromQuery = false;
        if (!string.IsNullOrEmpty(tamaraOrderId) && tamaraOrderId != order.PaymentIntentId)
        {
            if (string.IsNullOrEmpty(order.PaymentIntentId))
            {
                effectiveTamaraOrderId = tamaraOrderId;
                fromQuery = true;
            }
            else
            {
                _logger.LogWarning(
                    "Tamara return for order #{OrderId} carried Tamara order {QueryId}, but {StoredId} is stored; using the stored id.",
                    order.Id,
                    tamaraOrderId,
                    order.PaymentIntentId);
            }
        }

        var verified = await VerifyTamaraAsync(order, effectiveTamaraOrderId, cancellationToken, requireReference: fromQuery);
        if (!verified.IsPaid)
        {
            return new CompletePaymentResponse
            {
                OrderId = order.Id,
                OrderStatus = order.OrderStatus ?? OrderStatuses.Pending,
                PaymentStatus = order.PaymentStatus ?? PaymentStatuses.Pending,
                IsPaid = false,
                Message = verified.Message
            };
        }

        await MarkPaidAndFulfillAsync(order, userId, guestCartId, cancellationToken);

        return new CompletePaymentResponse
        {
            OrderId = order.Id,
            OrderStatus = order.OrderStatus ?? OrderStatuses.Paid,
            PaymentStatus = order.PaymentStatus ?? PaymentStatuses.Paid,
            IsPaid = true
        };
    }

    private async Task<CompletePaymentResponse> CompleteTabbyReturnAsync(
        int orderId,
        string status,
        string? paymentId,
        string? userId,
        string? guestCartId,
        CancellationToken cancellationToken)
    {
        var order = await _db.OrderHeaders.FirstOrDefaultAsync(o => o.Id == orderId, cancellationToken)
            ?? throw new InvalidOperationException("Order not found.");

        // The status in the return URL is only a hint and is never trusted: anyone can edit
        // it. The payment is always verified with Tabby, server to server, and must belong
        // to this order and match its total.
        var storedPaymentId = order.SessionId;
        var candidatePaymentId = !string.IsNullOrEmpty(paymentId) ? paymentId : storedPaymentId;
        var fromQuery = !string.Equals(candidatePaymentId, storedPaymentId, StringComparison.Ordinal);

        TappyPaymentStatusResponse? tabbyVerdict = null;
        var isPaid = false;
        var belongsToOrder = false;
        if (!string.IsNullOrEmpty(candidatePaymentId))
        {
            var helper = new TappyHelper(_tabby);
            tabbyVerdict = await helper.VerifyPaymentAsync(candidatePaymentId);
            belongsToOrder = TabbyPaymentBelongsTo(tabbyVerdict, order.Id.ToString(), requireReference: fromQuery);
            isPaid = belongsToOrder
                && EvaluateTabbyVerdict(tabbyVerdict, (decimal)order.OrderTotal, order.Id.ToString(), "order", order.Id).IsPaid;

            if (isPaid && fromQuery)
            {
                order.SessionId = candidatePaymentId;
                order.PaymentIntentId = candidatePaymentId;
                await _db.SaveChangesAsync(cancellationToken);
            }
        }

        _logger.LogInformation(
            "Tabby return for order {OrderId}: return status {ReturnStatus}, Tabby status {TabbyStatus}, verified paid {IsPaid}",
            order.Id,
            status,
            tabbyVerdict?.Status,
            isPaid);

        if (!isPaid)
        {
            // Close the order now only when Tabby itself says this order's payment failed.
            // (Tabby's CLOSED means captured, so it is deliberately not treated as a failure.)
            var reported = tabbyVerdict?.Status ?? string.Empty;
            if (tabbyVerdict is { Success: true }
                && belongsToOrder
                && !IsPaid(order)
                && (reported.Contains("reject", StringComparison.OrdinalIgnoreCase)
                    || reported.Contains("expire", StringComparison.OrdinalIgnoreCase)
                    || reported.Contains("cancel", StringComparison.OrdinalIgnoreCase)))
            {
                await CancelUnpaidOrderAsync(order, cancellationToken, $"Tabby reports the payment as \"{reported}\".");
            }

            return new CompletePaymentResponse
            {
                OrderId = order.Id,
                OrderStatus = order.OrderStatus ?? OrderStatuses.Pending,
                PaymentStatus = order.PaymentStatus ?? PaymentStatuses.Pending,
                IsPaid = false,
                Message = "Tabby payment was not completed."
            };
        }

        await MarkPaidAndFulfillAsync(order, userId, guestCartId, cancellationToken);

        return new CompletePaymentResponse
        {
            OrderId = order.Id,
            OrderStatus = order.OrderStatus ?? OrderStatuses.Paid,
            PaymentStatus = order.PaymentStatus ?? PaymentStatuses.Paid,
            IsPaid = true
        };
    }

    private async Task<(bool IsPaid, string? Message)> VerifyGeideaAsync(
        OrderHeader order,
        CancellationToken cancellationToken)
    {
        var helper = new GeideaHelper(_geidea);
        var result = await helper.VerifyPaymentAsync(order.Id.ToString());
        if (!result.Success)
            return (false, result.Message);

        if (result.IsPaid)
        {
            var amountCheck = ValidateGeideaPaidAmount(
                (decimal)order.OrderTotal,
                result.PaidAmount,
                order.Id,
                "order");
            if (!amountCheck.Ok)
                return (false, amountCheck.Error);
        }

        return (result.IsPaid, result.Message);
    }

    /// <param name="requireReference">
    /// True when <paramref name="tamaraOrderIdOverride"/> came from the customer (return URL)
    /// rather than from us: the Tamara order must then report this order as its reference,
    /// and nothing is authorised unless that is confirmed.
    /// </param>
    private async Task<(bool IsPaid, string? Message)> VerifyTamaraAsync(
        OrderHeader order,
        string? tamaraOrderIdOverride,
        CancellationToken cancellationToken,
        bool requireReference = false)
    {
        var helper = new TamaraHelper(_tamara);
        var tamaraOrderId = tamaraOrderIdOverride
            ?? order.PaymentIntentId
            ?? order.SessionId;

        if (string.IsNullOrEmpty(tamaraOrderId))
            return (false, "Tamara payment reference not found.");

        var details = await helper.GetOrderDetailsAsync(tamaraOrderId);

        if (requireReference
            && !PaymentVerificationRules.TamaraOrderBelongsTo(details.Success, details.OrderReferenceId, order.Id.ToString()))
        {
            _logger.LogWarning(
                "Tamara order {TamaraOrderId} from the return URL was not accepted for order #{OrderId}: reference {Reference}.",
                tamaraOrderId,
                order.Id,
                details.OrderReferenceId);
            return (false, "Tamara order could not be matched to this order.");
        }

        if (details.Success)
        {
            var status = details.Status?.ToLowerInvariant() ?? string.Empty;
            var paymentStatus = details.PaymentStatus?.ToLowerInvariant() ?? string.Empty;
            var approved = status.Contains("approved", StringComparison.Ordinal)
                || status.Contains("authorised", StringComparison.Ordinal)
                || paymentStatus.Contains("approved", StringComparison.Ordinal)
                || paymentStatus.Contains("authorised", StringComparison.Ordinal);

            if (approved)
            {
                if (!string.IsNullOrEmpty(details.OrderId))
                    order.PaymentIntentId = details.OrderId;

                var amountCheck = ValidateTamaraPaidAmount(
                    (decimal)order.OrderTotal,
                    details.TotalAmount,
                    order.Id,
                    "order");
                if (!amountCheck.Ok)
                    return (false, amountCheck.Error);

                if (!status.Contains("authorised", StringComparison.Ordinal)
                    && !paymentStatus.Contains("authorised", StringComparison.Ordinal))
                {
                    await helper.AuthorizeOrderAsync(tamaraOrderId);
                }

                return (true, "Tamara payment approved.");
            }
        }

        var auth = await helper.AuthorizeOrderAsync(tamaraOrderId);
        return auth.Success
            ? (true, "Tamara payment authorized.")
            : (false, details.Message ?? auth.Message ?? "Tamara payment verification failed.");
    }

    private async Task<(bool IsPaid, string? Message)> VerifyTabbyAsync(
        OrderHeader order,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(order.SessionId))
            return (false, "Tabby payment reference not found.");

        var helper = new TappyHelper(_tabby);
        var result = await helper.VerifyPaymentAsync(order.SessionId);
        return EvaluateTabbyVerdict(result, (decimal)order.OrderTotal, order.Id.ToString(), "order", order.Id);
    }

    private static bool TabbyPaymentBelongsTo(
        TappyPaymentStatusResponse result,
        string expectedReference,
        bool requireReference) =>
        PaymentVerificationRules.TabbyPaymentBelongsTo(result, expectedReference, requireReference);

    private (bool IsPaid, string? Message) EvaluateTabbyVerdict(
        TappyPaymentStatusResponse result,
        decimal expectedAmount,
        string expectedReference,
        string entityType,
        int entityId)
    {
        var verdict = PaymentVerificationRules.CheckTabbyVerdict(result, expectedAmount, expectedReference);
        if (!verdict.IsPaid && result is { Success: true, IsPaid: true })
        {
            _logger.LogWarning(
                "Tabby payment {PaymentId} rejected for {EntityType} {EntityId}: {Reason} (reference {Reference}, amount {Amount}, expected {Expected})",
                result.TransactionId,
                entityType,
                entityId,
                verdict.Message,
                result.OrderReferenceId,
                result.Amount,
                expectedAmount);
        }

        return verdict;
    }

    internal async Task MarkPaidAndFulfillAsync(
        OrderHeader order,
        string? userId,
        string? guestCartId,
        CancellationToken cancellationToken)
    {
        if (IsPaid(order))
            return;

        // Persist anything the verification step changed (e.g. the gateway order id) first.
        await _db.SaveChangesAsync(cancellationToken);

        var oldOrderStatus = order.OrderStatus;
        var oldPaymentStatus = order.PaymentStatus;
        var now = _clock.Now;

        // The return page, the webhook and the background job can all confirm the same
        // order at the same moment. The status change is therefore an atomic conditional
        // UPDATE ("claim"): only the caller whose UPDATE hits the row does the side effects,
        // and stock + promo run in the same transaction so a failure rolls the claim back
        // and a later attempt can retry cleanly.
        await using (var transaction = await _db.Database.BeginTransactionAsync(cancellationToken))
        {
            var claimed = await _db.OrderHeaders
                .Where(o => o.Id == order.Id
                    && (o.PaymentStatus == null || PaymentClaimableStatuses.Contains(o.PaymentStatus)))
                .ExecuteUpdateAsync(s => s
                    .SetProperty(o => o.PaymentStatus, PaymentStatuses.Paid)
                    .SetProperty(o => o.OrderStatus, OrderStatuses.Paid)
                    .SetProperty(o => o.PaymentDate, now),
                    cancellationToken);

            if (claimed == 0)
            {
                await transaction.RollbackAsync(cancellationToken);
                await _db.Entry(order).ReloadAsync(cancellationToken);
                _logger.LogInformation(
                    "Order #{OrderId} payment confirmation skipped: already handled (payment status {PaymentStatus}).",
                    order.Id,
                    order.PaymentStatus);
                return;
            }

            SyncTrackedStatus(order, PaymentStatuses.Paid, OrderStatuses.Paid, now);

            await _inventory.DeductStockForOrderAsync(order.Id, cancellationToken);

            if (order.PromoCodeId is > 0 && !string.IsNullOrEmpty(order.ApplicationUserId))
            {
                await _promoCodes.RecordUsageAsync(
                    order.PromoCodeId.Value,
                    order.ApplicationUserId,
                    order.Id,
                    cancellationToken);
            }

            await transaction.CommitAsync(cancellationToken);
        }

        if (oldOrderStatus == OrderStatuses.Cancelled)
        {
            _logger.LogWarning(
                "Order #{OrderId} was paid after it had been cancelled automatically; it has been reopened as Paid.",
                order.Id);
        }

        await AuditSystemAsync(
            order.Id,
            "Payment Confirmed",
            oldOrderStatus == OrderStatuses.Cancelled
                ? $"{order.PaymentMethod} payment confirmed by the gateway after the order had been cancelled automatically. Please review stock and fulfilment."
                : $"{order.PaymentMethod} payment confirmed by the gateway.",
            oldOrderStatus,
            order.OrderStatus,
            oldPaymentStatus,
            order.PaymentStatus,
            cancellationToken);

        await _cart.ClearCartAsync(userId ?? order.ApplicationUserId, guestCartId, cancellationToken);

        try
        {
            await _orderNotifications.SendOrderConfirmationAsync(order.Id, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to send order confirmation for order #{OrderId}", order.Id);
        }
    }

    public async Task<PaymentInitiationResult> InitiateServicePurchaseAsync(
        ServicePurchase purchase,
        ServiceSubscription service,
        CreateServicePurchaseRequest request,
        CancellationToken cancellationToken = default)
    {
        var paymentMethod = purchase.PaymentIntentId ?? request.PaymentMethod;
        return paymentMethod switch
        {
            PaymentMethods.Geidea => await InitiateServiceGeideaAsync(purchase, service, request, cancellationToken),
            PaymentMethods.Tamara => await InitiateServiceTamaraAsync(purchase, service, request, cancellationToken),
            PaymentMethods.Tabby => await InitiateServiceTabbyAsync(purchase, service, request, cancellationToken),
            _ => throw new InvalidOperationException("Unsupported online payment method.")
        };
    }

    public async Task<CompleteServicePaymentResponse> CompleteServicePurchaseAsync(
        int purchaseId,
        CancellationToken cancellationToken = default)
    {
        var purchase = await _db.ServicePurchases
            .Include(p => p.ServiceSubscription)
            .FirstOrDefaultAsync(p => p.Id == purchaseId, cancellationToken)
            ?? throw new InvalidOperationException("Service purchase not found.");

        if (IsServicePaid(purchase))
        {
            return new CompleteServicePaymentResponse
            {
                PurchaseId = purchase.Id,
                PaymentStatus = purchase.PaymentStatus,
                IsPaid = true,
                Message = "Payment already completed."
            };
        }

        var verified = purchase.PaymentIntentId switch
        {
            PaymentMethods.Geidea => await VerifyServiceGeideaAsync(purchase, cancellationToken),
            PaymentMethods.Tamara => await VerifyServiceTamaraAsync(purchase, null, cancellationToken),
            PaymentMethods.Tabby => await VerifyServiceTabbyAsync(purchase, cancellationToken),
            _ => throw new InvalidOperationException("Service purchase does not require online payment completion.")
        };

        if (!verified.IsPaid)
        {
            return new CompleteServicePaymentResponse
            {
                PurchaseId = purchase.Id,
                PaymentStatus = purchase.PaymentStatus,
                IsPaid = false,
                Message = verified.Message
            };
        }

        await MarkServicePurchasePaidAsync(purchase, cancellationToken);

        return new CompleteServicePaymentResponse
        {
            PurchaseId = purchase.Id,
            PaymentStatus = purchase.PaymentStatus,
            IsPaid = true,
            Message = verified.Message
        };
    }

    public async Task HandleServiceGeideaCallbackAsync(int purchaseId, CancellationToken cancellationToken = default)
    {
        var purchase = await _db.ServicePurchases
            .FirstOrDefaultAsync(p => p.Id == purchaseId, cancellationToken);

        if (purchase is null || IsServicePaid(purchase))
            return;

        // Same rule as product orders: unauthenticated callback = "check now" only.
        var result = await new GeideaHelper(_geidea).VerifyPaymentAsync(purchase.Id.ToString());

        if (result is { Success: true, IsPaid: true })
        {
            var amountCheck = ValidateGeideaPaidAmount(purchase.AmountPaid, result.PaidAmount, purchase.Id, "service purchase");
            if (amountCheck.Ok)
                await MarkServicePurchasePaidAsync(purchase, cancellationToken);
            return;
        }

        if (PaymentVerificationRules.GeideaIndicatesDefiniteFailure(result))
        {
            purchase.PaymentStatus = PaymentStatuses.Rejected;
            purchase.Status = "Cancelled";
            await _db.SaveChangesAsync(cancellationToken);
        }
    }

    public Task<CompleteServicePaymentResponse> HandleServiceTamaraReturnAsync(
        int purchaseId,
        string status,
        string? tamaraOrderId,
        CancellationToken cancellationToken = default)
    {
        if (!status.Equals("success", StringComparison.OrdinalIgnoreCase))
        {
            return Task.FromResult(new CompleteServicePaymentResponse
            {
                PurchaseId = purchaseId,
                PaymentStatus = PaymentStatuses.Pending,
                IsPaid = false,
                Message = "Payment was not completed."
            });
        }

        return CompleteServiceTamaraReturnAsync(purchaseId, tamaraOrderId, cancellationToken);
    }

    public Task<CompleteServicePaymentResponse> HandleServiceTabbyReturnAsync(
        int purchaseId,
        string status,
        string? paymentId,
        CancellationToken cancellationToken = default) =>
        CompleteServiceTabbyReturnAsync(purchaseId, status, paymentId, cancellationToken);

    private async Task<PaymentInitiationResult> InitiateServiceGeideaAsync(
        ServicePurchase purchase,
        ServiceSubscription service,
        CreateServicePurchaseRequest request,
        CancellationToken cancellationToken)
    {
        var apiBase = _urls.PublicApiBaseUrl.TrimEnd('/');
        var callbackBase = string.IsNullOrWhiteSpace(_geidea.CallbackUrlOverride)
            ? apiBase
            : _geidea.CallbackUrlOverride.TrimEnd('/');

        var callbackUrl = $"{callbackBase}/api/payments/geidea/service-callback?purchaseId={purchase.Id}";
        var frontend = _urls.FrontendBaseUrl.TrimEnd('/');

        if ((callbackUrl.Contains("localhost", StringComparison.OrdinalIgnoreCase)
                || callbackUrl.Contains("127.0.0.1", StringComparison.OrdinalIgnoreCase)
                || !callbackUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            && string.IsNullOrWhiteSpace(_geidea.CallbackUrlOverride))
        {
            throw new InvalidOperationException(
                "Geidea requires a public HTTPS callback URL. Set Geidea:CallbackUrlOverride in appsettings (e.g. ngrok URL) for local testing.");
        }

        var helper = new GeideaHelper(_geidea);
        var response = await helper.CreatePaymentAsync(new GeideaPaymentRequest
        {
            Amount = purchase.AmountPaid,
            Currency = "AED",
            OrderId = purchase.Id.ToString(),
            CustomerName = request.Name,
            CustomerEmail = request.Email,
            CustomerPhone = request.PhoneNumber,
            ReturnUrl = callbackUrl,
            CancelUrl = $"{frontend}/services/{service.Id}/checkout"
        });

        if (!response.Success || string.IsNullOrEmpty(response.TransactionId))
            throw new InvalidOperationException(response.Message ?? "Failed to create Geidea payment session.");

        purchase.SessionId = response.TransactionId;
        purchase.PaymentIntentId = PaymentMethods.Geidea;
        await _db.SaveChangesAsync(cancellationToken);

        return new PaymentInitiationResult
        {
            SessionId = response.TransactionId,
            RedirectUrl = string.IsNullOrEmpty(response.PaymentUrl) ? null : response.PaymentUrl
        };
    }

    private async Task<PaymentInitiationResult> InitiateServiceTamaraAsync(
        ServicePurchase purchase,
        ServiceSubscription service,
        CreateServicePurchaseRequest request,
        CancellationToken cancellationToken)
    {
        if (!_tamara.Enabled)
            throw new InvalidOperationException("Tamara payment is not available.");

        var apiBase = _urls.PublicApiBaseUrl.TrimEnd('/');
        var currency = _tamara.Currency ?? "AED";
        var countryCode = _tamara.CountryCode ?? "AE";
        var nameParts = request.Name.Trim().Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
        var firstName = nameParts[0];
        var lastName = nameParts.Length > 1 ? nameParts[1] : firstName;
        var phone = FormatTamaraPhone(request.PhoneNumber);
        var title = service.Title.Length > 200 ? service.Title[..200] : service.Title;

        var helper = new TamaraHelper(_tamara);
        var tamaraRequest = new TamaraPaymentRequest
        {
            OrderReferenceId = purchase.Id.ToString(),
            OrderNumber = $"SRV-{purchase.Id}",
            TotalAmount = new TamaraAmount { Amount = purchase.AmountPaid, Currency = currency },
            Description = title,
            CountryCode = countryCode,
            PaymentType = "PAY_BY_INSTALMENTS",
            Locale = "en_AE",
            Platform = "IdealWeightNutrition.Api",
            MerchantUrl = new TamaraMerchantUrl
            {
                Success = $"{apiBase}/api/payments/tamara/service-return?purchaseId={purchase.Id}&status=success",
                Failure = $"{apiBase}/api/payments/tamara/service-return?purchaseId={purchase.Id}&status=failure",
                Cancel = $"{_urls.FrontendBaseUrl.TrimEnd('/')}/services/{service.Id}/checkout",
                Notification = $"{apiBase}/api/payments/tamara/webhook"
            },
            Consumer = new TamaraConsumer
            {
                FirstName = firstName,
                LastName = lastName,
                PhoneNumber = phone,
                Email = request.Email
            },
            BillingAddress = BuildServiceTamaraAddress(firstName, lastName, phone, countryCode),
            ShippingAddress = BuildServiceTamaraAddress(firstName, lastName, phone, countryCode),
            Items =
            [
                new TamaraItem
                {
                    ReferenceId = service.Id.ToString(),
                    Type = "Digital",
                    Name = title,
                    Sku = service.Id.ToString(),
                    Quantity = 1,
                    UnitPrice = new TamaraAmount { Amount = purchase.AmountPaid, Currency = currency },
                    TotalAmount = new TamaraAmount { Amount = purchase.AmountPaid, Currency = currency },
                    DiscountAmount = new TamaraAmount { Amount = 0, Currency = currency },
                    TaxAmount = new TamaraAmount { Amount = 0, Currency = currency }
                }
            ],
            TaxAmount = new TamaraAmount { Amount = 0, Currency = currency },
            ShippingAmount = new TamaraAmount { Amount = 0, Currency = currency }
        };

        if (purchase.DiscountAmount > 0)
        {
            tamaraRequest.Discount = new TamaraDiscount
            {
                Name = "Discount",
                Amount = new TamaraAmount { Amount = purchase.DiscountAmount, Currency = currency }
            };
        }

        var response = await helper.CreateCheckoutAsync(tamaraRequest);
        if (!response.Success || string.IsNullOrEmpty(response.CheckoutUrl))
            throw new InvalidOperationException(response.Message ?? "Failed to create Tamara checkout.");

        purchase.SessionId = response.OrderId ?? response.CheckoutId;
        purchase.PaymentIntentId = PaymentMethods.Tamara;
        await _db.SaveChangesAsync(cancellationToken);

        return new PaymentInitiationResult { RedirectUrl = response.CheckoutUrl };
    }

    private async Task<PaymentInitiationResult> InitiateServiceTabbyAsync(
        ServicePurchase purchase,
        ServiceSubscription service,
        CreateServicePurchaseRequest request,
        CancellationToken cancellationToken)
    {
        var apiBase = _urls.PublicApiBaseUrl.TrimEnd('/');
        var frontend = _urls.FrontendBaseUrl.TrimEnd('/');
        var helper = new TappyHelper(_tabby);
        var title = service.Title.Length > 200 ? service.Title[..200] : service.Title;
        var imageUrl = string.IsNullOrWhiteSpace(service.ImageUrl)
            ? $"{frontend}/favicon.ico"
            : service.ImageUrl.StartsWith("http", StringComparison.OrdinalIgnoreCase)
                ? service.ImageUrl
                : $"{frontend}/{service.ImageUrl.TrimStart('/')}";

        var response = await helper.CreatePaymentAsync(new TappyPaymentRequest
        {
            MerchantId = _tabby.MerchantId,
            Amount = purchase.AmountPaid,
            Currency = "AED",
            OrderId = purchase.Id.ToString(),
            CustomerName = request.Name,
            CustomerEmail = request.Email,
            CustomerPhone = request.PhoneNumber,
            ReturnUrl = $"{apiBase}/api/payments/tabby/service-return?purchaseId={purchase.Id}",
            CancelUrl = $"{frontend}/services/{service.Id}/checkout",
            Description = title,
            ShippingCity = "Dubai",
            ShippingAddress = "UAE",
            ShippingPostalCode = "00000",
            DiscountAmount = purchase.DiscountAmount > 0 ? purchase.DiscountAmount : null,
            Language = "en",
            Items =
            [
                new TabbyOrderItem
                {
                    ReferenceId = service.Id.ToString(),
                    Title = title,
                    Description = service.Description ?? title,
                    Quantity = 1,
                    UnitPrice = purchase.AmountPaid,
                    DiscountAmount = 0,
                    ImageUrl = imageUrl,
                    ProductUrl = $"{frontend}/services/{service.Id}",
                    Category = "Services"
                }
            ]
        });

        if (!response.Success || string.IsNullOrEmpty(response.PaymentUrl))
            throw new InvalidOperationException(response.Message ?? "Failed to create Tabby payment.");

        purchase.SessionId = response.TransactionId;
        purchase.PaymentIntentId = PaymentMethods.Tabby;
        await _db.SaveChangesAsync(cancellationToken);

        return new PaymentInitiationResult { RedirectUrl = response.PaymentUrl };
    }

    private async Task<CompleteServicePaymentResponse> CompleteServiceTamaraReturnAsync(
        int purchaseId,
        string? tamaraOrderId,
        CancellationToken cancellationToken)
    {
        var purchase = await _db.ServicePurchases
            .FirstOrDefaultAsync(p => p.Id == purchaseId, cancellationToken)
            ?? throw new InvalidOperationException("Service purchase not found.");

        // A Tamara order id from the return URL that differs from the stored one is only
        // accepted if Tamara confirms it was created for this purchase.
        var fromQuery = !string.IsNullOrEmpty(tamaraOrderId) && tamaraOrderId != purchase.SessionId;
        var verified = await VerifyServiceTamaraAsync(
            purchase,
            fromQuery ? tamaraOrderId : null,
            cancellationToken,
            requireReference: fromQuery);

        if (verified.IsPaid && fromQuery)
            purchase.SessionId = tamaraOrderId;
        if (!verified.IsPaid)
        {
            return new CompleteServicePaymentResponse
            {
                PurchaseId = purchase.Id,
                PaymentStatus = purchase.PaymentStatus,
                IsPaid = false,
                Message = verified.Message
            };
        }

        await MarkServicePurchasePaidAsync(purchase, cancellationToken);

        return new CompleteServicePaymentResponse
        {
            PurchaseId = purchase.Id,
            PaymentStatus = purchase.PaymentStatus,
            IsPaid = true,
            Message = verified.Message
        };
    }

    private async Task<CompleteServicePaymentResponse> CompleteServiceTabbyReturnAsync(
        int purchaseId,
        string status,
        string? paymentId,
        CancellationToken cancellationToken)
    {
        var purchase = await _db.ServicePurchases
            .FirstOrDefaultAsync(p => p.Id == purchaseId, cancellationToken)
            ?? throw new InvalidOperationException("Service purchase not found.");

        // Never trust the status in the return URL; always verify with Tabby (see the
        // product-order return for the reasoning).
        var storedPaymentId = purchase.SessionId;
        var candidatePaymentId = !string.IsNullOrEmpty(paymentId) ? paymentId : storedPaymentId;
        var fromQuery = !string.Equals(candidatePaymentId, storedPaymentId, StringComparison.Ordinal);

        var isPaid = false;
        if (!string.IsNullOrEmpty(candidatePaymentId))
        {
            var helper = new TappyHelper(_tabby);
            var verification = await helper.VerifyPaymentAsync(candidatePaymentId);
            isPaid = TabbyPaymentBelongsTo(verification, purchase.Id.ToString(), requireReference: fromQuery)
                && EvaluateTabbyVerdict(verification, purchase.AmountPaid, purchase.Id.ToString(), "service purchase", purchase.Id).IsPaid;

            if (isPaid && fromQuery)
                purchase.SessionId = candidatePaymentId;
        }

        _logger.LogInformation(
            "Tabby return for service purchase {PurchaseId}: return status {ReturnStatus}, verified paid {IsPaid}",
            purchase.Id,
            status,
            isPaid);

        if (!isPaid)
        {
            return new CompleteServicePaymentResponse
            {
                PurchaseId = purchase.Id,
                PaymentStatus = purchase.PaymentStatus,
                IsPaid = false,
                Message = "Tabby payment was not completed."
            };
        }

        await MarkServicePurchasePaidAsync(purchase, cancellationToken);

        return new CompleteServicePaymentResponse
        {
            PurchaseId = purchase.Id,
            PaymentStatus = purchase.PaymentStatus,
            IsPaid = true
        };
    }

    private async Task<(bool IsPaid, string? Message)> VerifyServiceGeideaAsync(
        ServicePurchase purchase,
        CancellationToken cancellationToken)
    {
        var helper = new GeideaHelper(_geidea);
        var result = await helper.VerifyPaymentAsync(purchase.Id.ToString());
        if (!result.Success)
            return (false, result.Message);

        if (result.IsPaid)
        {
            var amountCheck = ValidateGeideaPaidAmount(
                purchase.AmountPaid,
                result.PaidAmount,
                purchase.Id,
                "service purchase");
            if (!amountCheck.Ok)
                return (false, amountCheck.Error);
        }

        return (result.IsPaid, result.Message);
    }

    private async Task<(bool IsPaid, string? Message)> VerifyServiceTamaraAsync(
        ServicePurchase purchase,
        string? tamaraOrderIdOverride,
        CancellationToken cancellationToken,
        bool requireReference = false)
    {
        var helper = new TamaraHelper(_tamara);
        var tamaraOrderId = tamaraOrderIdOverride ?? purchase.SessionId;

        if (string.IsNullOrEmpty(tamaraOrderId))
            return (false, "Tamara payment reference not found.");

        var details = await helper.GetOrderDetailsAsync(tamaraOrderId);

        if (requireReference
            && !PaymentVerificationRules.TamaraOrderBelongsTo(details.Success, details.OrderReferenceId, purchase.Id.ToString()))
        {
            _logger.LogWarning(
                "Tamara order {TamaraOrderId} from the return URL was not accepted for service purchase {PurchaseId}: reference {Reference}.",
                tamaraOrderId,
                purchase.Id,
                details.OrderReferenceId);
            return (false, "Tamara order could not be matched to this purchase.");
        }

        if (details.Success)
        {
            var status = details.Status?.ToLowerInvariant() ?? string.Empty;
            var paymentStatus = details.PaymentStatus?.ToLowerInvariant() ?? string.Empty;
            var approved = status.Contains("approved", StringComparison.Ordinal)
                || status.Contains("authorised", StringComparison.Ordinal)
                || paymentStatus.Contains("approved", StringComparison.Ordinal)
                || paymentStatus.Contains("authorised", StringComparison.Ordinal);

            if (approved)
            {
                var amountCheck = ValidateTamaraPaidAmount(
                    purchase.AmountPaid,
                    details.TotalAmount,
                    purchase.Id,
                    "service purchase");
                if (!amountCheck.Ok)
                    return (false, amountCheck.Error);

                if (!status.Contains("authorised", StringComparison.Ordinal)
                    && !paymentStatus.Contains("authorised", StringComparison.Ordinal))
                {
                    await helper.AuthorizeOrderAsync(tamaraOrderId);
                }

                return (true, "Tamara payment approved.");
            }
        }

        var auth = await helper.AuthorizeOrderAsync(tamaraOrderId);
        return auth.Success
            ? (true, "Tamara payment authorized.")
            : (false, details.Message ?? auth.Message ?? "Tamara payment verification failed.");
    }

    private async Task<(bool IsPaid, string? Message)> VerifyServiceTabbyAsync(
        ServicePurchase purchase,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(purchase.SessionId))
            return (false, "Tabby payment reference not found.");

        var helper = new TappyHelper(_tabby);
        var result = await helper.VerifyPaymentAsync(purchase.SessionId);
        return EvaluateTabbyVerdict(result, purchase.AmountPaid, purchase.Id.ToString(), "service purchase", purchase.Id);
    }

    internal async Task MarkServicePurchasePaidAsync(
        ServicePurchase purchase,
        CancellationToken cancellationToken)
    {
        if (IsServicePaid(purchase))
            return;

        await _db.SaveChangesAsync(cancellationToken);

        // Atomic claim, so concurrent confirmations approve the purchase exactly once.
        var now = _clock.Now;
        var claimed = await _db.ServicePurchases
            .Where(p => p.Id == purchase.Id
                && p.PaymentStatus != ServicePaymentApproved
                && p.PaymentStatus != PaymentStatuses.Paid
                && p.PaymentStatus != PaymentStatuses.Refunded)
            .ExecuteUpdateAsync(s => s
                .SetProperty(p => p.PaymentStatus, ServicePaymentApproved)
                .SetProperty(p => p.PurchaseDate, now),
                cancellationToken);

        if (claimed == 0)
        {
            await _db.Entry(purchase).ReloadAsync(cancellationToken);
            return;
        }

        var entry = _db.Entry(purchase);
        entry.Property(p => p.PaymentStatus).CurrentValue = ServicePaymentApproved;
        entry.Property(p => p.PaymentStatus).OriginalValue = ServicePaymentApproved;
        entry.Property(p => p.PurchaseDate).CurrentValue = now;
        entry.Property(p => p.PurchaseDate).OriginalValue = now;
    }

    private static bool IsServicePaid(ServicePurchase purchase) =>
        purchase.PaymentStatus is ServicePaymentApproved or PaymentStatuses.Paid;

    private static TamaraAddress BuildServiceTamaraAddress(
        string firstName,
        string lastName,
        string phone,
        string countryCode) =>
        new()
        {
            FirstName = firstName,
            LastName = lastName,
            Line1 = "UAE",
            City = "Dubai",
            Region = "Dubai",
            PostalCode = "00000",
            CountryCode = countryCode,
            PhoneNumber = phone
        };

    private static bool IsPaid(OrderHeader order) =>
        order.PaymentStatus is PaymentStatuses.Paid or PaymentStatuses.DelayedPayment;

    private static TamaraAddress BuildTamaraAddress(
        string firstName,
        string lastName,
        OrderHeader order,
        string phone,
        string countryCode,
        string postalCode) =>
        new()
        {
            FirstName = firstName,
            LastName = lastName,
            Line1 = order.StreetAddress,
            City = order.City,
            Region = order.State,
            PostalCode = postalCode,
            CountryCode = countryCode,
            PhoneNumber = phone
        };

    private static string FormatTamaraPhone(string phone)
    {
        var digits = new string(phone.Where(char.IsDigit).ToArray());
        if (digits.StartsWith("971", StringComparison.Ordinal))
            digits = digits[3..];
        if (digits.StartsWith('0'))
            digits = digits[1..];
        return digits;
    }

    public async Task VerifyPendingPaymentsAsync(CancellationToken cancellationToken = default)
    {
        if (!_paymentVerification.Enabled)
            return;

        var now = _clock.Now;
        var threshold = now.AddMinutes(-_paymentVerification.PendingOrderThresholdMinutes);

        var pendingOrders = await _db.OrderHeaders
            .Where(o =>
                o.PaymentStatus == PaymentStatuses.Pending
                && o.OrderDate < threshold
                && o.PaymentMethod != null
                && o.PaymentMethod != PaymentMethods.Cod)
            .ToListAsync(cancellationToken);

        foreach (var order in pendingOrders)
            await VerifyPendingOrderAsync(order, now, cancellationToken);

        var pendingPurchases = await _db.ServicePurchases
            .Where(p =>
                p.PaymentStatus == PaymentStatuses.Pending
                && p.PurchaseDate < threshold
                && p.PaymentIntentId != null
                && p.PaymentIntentId != string.Empty)
            .ToListAsync(cancellationToken);

        foreach (var purchase in pendingPurchases)
            await VerifyPendingServicePurchaseAsync(purchase, now, cancellationToken);
    }

    private async Task VerifyPendingOrderAsync(
        OrderHeader order,
        DateTime now,
        CancellationToken cancellationToken)
    {
        try
        {
            var verification = await VerifyPendingOrderPaymentAsync(order, cancellationToken);

            if (verification.IsPaid)
            {
                _logger.LogInformation(
                    "Payment verification: order #{OrderId} paid via {Method}. {Message}",
                    order.Id,
                    order.PaymentMethod,
                    verification.Message);

                await MarkPaidAndFulfillAsync(order, order.ApplicationUserId, null, cancellationToken);
                return;
            }

            if (verification.NotFound)
            {
                _logger.LogWarning(
                    "Payment verification: order #{OrderId} not found in {Method}. Cancelling. {Message}",
                    order.Id,
                    order.PaymentMethod,
                    verification.Message);

                await CancelUnpaidOrderAsync(order, cancellationToken);
                return;
            }

            var ageMinutes = (now - order.OrderDate).TotalMinutes;
            if (ageMinutes >= _paymentVerification.CancelAfterMinutes)
            {
                _logger.LogWarning(
                    "Payment verification: order #{OrderId} still unpaid after {Minutes} minutes. Cancelling.",
                    order.Id,
                    ageMinutes);

                await CancelUnpaidOrderAsync(order, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Payment verification failed for order #{OrderId}", order.Id);
        }
    }

    private async Task VerifyPendingServicePurchaseAsync(
        ServicePurchase purchase,
        DateTime now,
        CancellationToken cancellationToken)
    {
        try
        {
            var method = purchase.PaymentIntentId;
            if (method is not (PaymentMethods.Geidea or PaymentMethods.Tamara or PaymentMethods.Tabby))
                return;

            var verification = method switch
            {
                PaymentMethods.Geidea => await VerifyServiceGeideaWithNotFoundAsync(purchase, cancellationToken),
                PaymentMethods.Tamara => await VerifyServiceTamaraWithNotFoundAsync(purchase, cancellationToken),
                PaymentMethods.Tabby => await VerifyServiceTabbyWithNotFoundAsync(purchase, cancellationToken),
                _ => new PendingPaymentVerification(false, false, null)
            };

            if (verification.IsPaid)
            {
                _logger.LogInformation(
                    "Payment verification: service purchase #{PurchaseId} paid via {Method}.",
                    purchase.Id,
                    method);

                await MarkServicePurchasePaidAsync(purchase, cancellationToken);
                return;
            }

            if (verification.NotFound)
            {
                _logger.LogWarning(
                    "Payment verification: service purchase #{PurchaseId} not found in {Method}. Cancelling.",
                    purchase.Id,
                    method);

                await CancelUnpaidServicePurchaseAsync(purchase, cancellationToken);
                return;
            }

            var ageMinutes = (now - purchase.PurchaseDate).TotalMinutes;
            if (ageMinutes >= _paymentVerification.CancelAfterMinutes)
            {
                _logger.LogWarning(
                    "Payment verification: service purchase #{PurchaseId} still unpaid after {Minutes} minutes. Cancelling.",
                    purchase.Id,
                    ageMinutes);

                await CancelUnpaidServicePurchaseAsync(purchase, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Payment verification failed for service purchase #{PurchaseId}", purchase.Id);
        }
    }

    private async Task<PendingPaymentVerification> VerifyPendingOrderPaymentAsync(
        OrderHeader order,
        CancellationToken cancellationToken)
    {
        return order.PaymentMethod switch
        {
            PaymentMethods.Geidea => await VerifyGeideaWithNotFoundAsync(order, cancellationToken),
            PaymentMethods.Tamara => await VerifyTamaraWithNotFoundAsync(order, cancellationToken),
            PaymentMethods.Tabby => await VerifyTabbyWithNotFoundAsync(order, cancellationToken),
            _ => new PendingPaymentVerification(false, false, "Unsupported payment method")
        };
    }

    private async Task<PendingPaymentVerification> VerifyGeideaWithNotFoundAsync(
        OrderHeader order,
        CancellationToken cancellationToken)
    {
        var helper = new GeideaHelper(_geidea);
        var result = await helper.VerifyPaymentAsync(order.Id.ToString());
        if (!result.Success)
        {
            return new PendingPaymentVerification(
                false,
                PaymentMessageIndicatesNotFound(result.Message),
                result.Message);
        }

        if (result.IsPaid)
        {
            var amountCheck = ValidateGeideaPaidAmount(
                (decimal)order.OrderTotal,
                result.PaidAmount,
                order.Id,
                "order");
            if (!amountCheck.Ok)
                return new PendingPaymentVerification(false, false, amountCheck.Error);
        }

        return new PendingPaymentVerification(result.IsPaid, false, result.Message);
    }

    private async Task<PendingPaymentVerification> VerifyTamaraWithNotFoundAsync(
        OrderHeader order,
        CancellationToken cancellationToken)
    {
        var tamaraOrderId = order.PaymentIntentId ?? order.SessionId;
        if (string.IsNullOrEmpty(tamaraOrderId))
            return new PendingPaymentVerification(false, true, "Tamara order ID not found.");

        var verified = await VerifyTamaraAsync(order, tamaraOrderId, cancellationToken);
        if (verified.IsPaid)
            return new PendingPaymentVerification(true, false, verified.Message);

        var helper = new TamaraHelper(_tamara);
        var details = await helper.GetOrderDetailsAsync(tamaraOrderId);
        if (!details.Success)
        {
            return new PendingPaymentVerification(
                false,
                PaymentMessageIndicatesNotFound(details.Message),
                details.Message);
        }

        return new PendingPaymentVerification(false, false, verified.Message ?? details.Message);
    }

    private async Task<PendingPaymentVerification> VerifyTabbyWithNotFoundAsync(
        OrderHeader order,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(order.SessionId) && string.IsNullOrEmpty(order.PaymentIntentId))
            return new PendingPaymentVerification(false, true, "Tabby payment ID not found.");

        var verified = await VerifyTabbyAsync(order, cancellationToken);
        if (verified.IsPaid)
            return new PendingPaymentVerification(true, false, verified.Message);

        var helper = new TappyHelper(_tabby);
        var paymentId = order.SessionId ?? order.PaymentIntentId!;
        var result = await helper.VerifyPaymentAsync(paymentId);
        return new PendingPaymentVerification(
            false,
            !result.Success && PaymentMessageIndicatesNotFound(result.Message),
            result.Message ?? verified.Message);
    }

    private async Task<PendingPaymentVerification> VerifyServiceGeideaWithNotFoundAsync(
        ServicePurchase purchase,
        CancellationToken cancellationToken)
    {
        var helper = new GeideaHelper(_geidea);
        var result = await helper.VerifyPaymentAsync(purchase.Id.ToString());
        if (!result.Success)
        {
            return new PendingPaymentVerification(
                false,
                PaymentMessageIndicatesNotFound(result.Message),
                result.Message);
        }

        if (result.IsPaid)
        {
            var amountCheck = ValidateGeideaPaidAmount(
                purchase.AmountPaid,
                result.PaidAmount,
                purchase.Id,
                "service purchase");
            if (!amountCheck.Ok)
                return new PendingPaymentVerification(false, false, amountCheck.Error);
        }

        return new PendingPaymentVerification(result.IsPaid, false, result.Message);
    }

    private async Task<PendingPaymentVerification> VerifyServiceTamaraWithNotFoundAsync(
        ServicePurchase purchase,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(purchase.SessionId))
            return new PendingPaymentVerification(false, true, "Tamara order ID not found.");

        var verified = await VerifyServiceTamaraAsync(purchase, purchase.SessionId, cancellationToken);
        if (verified.IsPaid)
            return new PendingPaymentVerification(true, false, verified.Message);

        var helper = new TamaraHelper(_tamara);
        var details = await helper.GetOrderDetailsAsync(purchase.SessionId);
        if (!details.Success)
        {
            return new PendingPaymentVerification(
                false,
                PaymentMessageIndicatesNotFound(details.Message),
                details.Message);
        }

        return new PendingPaymentVerification(false, false, verified.Message ?? details.Message);
    }

    private async Task<PendingPaymentVerification> VerifyServiceTabbyWithNotFoundAsync(
        ServicePurchase purchase,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(purchase.SessionId))
            return new PendingPaymentVerification(false, true, "Tabby payment ID not found.");

        var verified = await VerifyServiceTabbyAsync(purchase, cancellationToken);
        if (verified.IsPaid)
            return new PendingPaymentVerification(true, false, verified.Message);

        var helper = new TappyHelper(_tabby);
        var result = await helper.VerifyPaymentAsync(purchase.SessionId);
        return new PendingPaymentVerification(
            false,
            !result.Success && PaymentMessageIndicatesNotFound(result.Message),
            result.Message ?? verified.Message);
    }

    /// <summary>Payment states from which an order may be claimed as Paid.</summary>
    /// <remarks>
    /// Rejected = cancelled automatically because the payment did not complete; a late
    /// payment may still arrive. Cancelled (by an admin), Refunded and the paid states are
    /// deliberately excluded so a replayed return/webhook can never re-open them.
    /// </remarks>
    private static readonly string[] PaymentClaimableStatuses =
    [
        PaymentStatuses.Pending,
        PaymentStatuses.Rejected,
        PaymentStatuses.Authorized
    ];

    /// <summary>Payment states from which an unpaid order may be cancelled automatically.</summary>
    private static readonly string[] PaymentCancellableStatuses =
    [
        PaymentStatuses.Pending,
        PaymentStatuses.Rejected
    ];

    /// <summary>
    /// Mirrors a status change made with ExecuteUpdate onto the tracked entity without
    /// marking it modified (the database already has these values).
    /// </summary>
    private void SyncTrackedStatus(OrderHeader order, string paymentStatus, string orderStatus, DateTime paymentDate)
    {
        var entry = _db.Entry(order);
        entry.Property(o => o.PaymentStatus).CurrentValue = paymentStatus;
        entry.Property(o => o.PaymentStatus).OriginalValue = paymentStatus;
        entry.Property(o => o.OrderStatus).CurrentValue = orderStatus;
        entry.Property(o => o.OrderStatus).OriginalValue = orderStatus;
        entry.Property(o => o.PaymentDate).CurrentValue = paymentDate;
        entry.Property(o => o.PaymentDate).OriginalValue = paymentDate;
    }

    internal async Task CancelUnpaidOrderAsync(
        OrderHeader order,
        CancellationToken cancellationToken,
        string? reason = null,
        string paymentStatus = PaymentStatuses.Rejected)
    {
        if (IsPaid(order) || order.OrderStatus == OrderStatuses.Cancelled)
            return;

        await _db.SaveChangesAsync(cancellationToken);

        var oldOrderStatus = order.OrderStatus;
        var oldPaymentStatus = order.PaymentStatus;
        var now = _clock.Now;

        // Atomic and conditional, like the paid claim: if a confirmation won the race a
        // moment ago this UPDATE matches nothing, so a paid order is never cancelled.
        var cancelled = await _db.OrderHeaders
            .Where(o => o.Id == order.Id
                && o.OrderStatus != OrderStatuses.Cancelled
                && (o.PaymentStatus == null || PaymentCancellableStatuses.Contains(o.PaymentStatus)))
            .ExecuteUpdateAsync(s => s
                .SetProperty(o => o.OrderStatus, OrderStatuses.Cancelled)
                .SetProperty(o => o.PaymentStatus, paymentStatus)
                .SetProperty(o => o.PaymentDate, now),
                cancellationToken);

        if (cancelled == 0)
        {
            await _db.Entry(order).ReloadAsync(cancellationToken);
            _logger.LogInformation(
                "Order #{OrderId} not cancelled: its status changed meanwhile (payment status {PaymentStatus}).",
                order.Id,
                order.PaymentStatus);
            return;
        }

        SyncTrackedStatus(order, paymentStatus, OrderStatuses.Cancelled, now);

        // Online orders record promo usage only once paid, so there is normally nothing to
        // release; this covers any usage recorded earlier.
        await _promoCodes.ReleaseUsageForOrderAsync(order.Id, cancellationToken);

        await AuditSystemAsync(
            order.Id,
            "Payment Not Completed",
            reason ?? $"{order.PaymentMethod} payment was not completed; order cancelled automatically.",
            oldOrderStatus,
            order.OrderStatus,
            oldPaymentStatus,
            order.PaymentStatus,
            cancellationToken);
    }

    /// <summary>
    /// Records a status change made by the system (gateway callback, webhook or background
    /// job) in the same audit trail admins see. Never throws.
    /// </summary>
    private async Task AuditSystemAsync(
        int orderId,
        string action,
        string? details,
        string? oldOrderStatus,
        string? newOrderStatus,
        string? oldPaymentStatus,
        string? newPaymentStatus,
        CancellationToken cancellationToken)
    {
        try
        {
            var now = _clock.Now;
            _db.OrderAuditLogs.Add(new OrderAuditLog
            {
                OrderHeaderId = orderId,
                Action = action,
                ActionDetails = details is { Length: > 500 } ? details[..500] : details,
                PerformedByUserEmail = "system",
                OldOrderStatus = oldOrderStatus,
                NewOrderStatus = newOrderStatus,
                OldPaymentStatus = oldPaymentStatus,
                NewPaymentStatus = newPaymentStatus,
                ActionDate = now,
                CreatedBy = "system",
                CreatedDate = now,
                IsDeleted = false
            });
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not write audit entry '{Action}' for order #{OrderId}", action, orderId);
        }
    }

    private async Task CancelUnpaidServicePurchaseAsync(
        ServicePurchase purchase,
        CancellationToken cancellationToken)
    {
        if (IsServicePaid(purchase) || purchase.Status == "Cancelled")
            return;

        purchase.PaymentStatus = PaymentStatuses.Rejected;
        purchase.Status = "Cancelled";
        await _db.SaveChangesAsync(cancellationToken);
    }

    private static bool PaymentMessageIndicatesNotFound(string? message)
    {
        if (string.IsNullOrWhiteSpace(message))
            return false;

        return message.Contains("no orders found", StringComparison.OrdinalIgnoreCase)
            || message.Contains("not found", StringComparison.OrdinalIgnoreCase)
            || message.Contains("does not exist", StringComparison.OrdinalIgnoreCase)
            || message.Contains("404", StringComparison.OrdinalIgnoreCase)
            || message.Contains("payment not found", StringComparison.OrdinalIgnoreCase);
    }

    private readonly record struct PendingPaymentVerification(bool IsPaid, bool NotFound, string? Message);
}
