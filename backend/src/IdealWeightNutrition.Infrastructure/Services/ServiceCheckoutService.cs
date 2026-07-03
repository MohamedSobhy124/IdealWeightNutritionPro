using IdealWeightNutrition.Application.Abstractions;
using IdealWeightNutrition.Contracts.Checkout;
using IdealWeightNutrition.Contracts.Services;
using IdealWeightNutrition.Domain.Constants;
using IdealWeightNutrition.Domain.Identity;
using IdealWeightNutrition.Domain.Promotions;
using IdealWeightNutrition.Domain.Services;
using IdealWeightNutrition.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace IdealWeightNutrition.Infrastructure.Services;

internal sealed class ServiceCheckoutService : IServiceCheckoutService
{
    private const string ServicePaymentApproved = "Approved";

    private readonly AppDbContext _db;
    private readonly IPaymentService _payments;
    private readonly IOtpService _otp;
    private readonly IEmailService _email;
    private readonly UserManager<ApplicationUser> _users;
    private readonly IGuestAccountService _guestAccounts;
    private readonly IInAppNotificationService _inAppNotifications;
    private readonly IDateTimeProvider _clock;
    private readonly ILogger<ServiceCheckoutService> _logger;

    public ServiceCheckoutService(
        AppDbContext db,
        IPaymentService payments,
        IOtpService otp,
        IEmailService email,
        UserManager<ApplicationUser> users,
        IGuestAccountService guestAccounts,
        IInAppNotificationService inAppNotifications,
        IDateTimeProvider clock,
        ILogger<ServiceCheckoutService> logger)
    {
        _db = db;
        _payments = payments;
        _otp = otp;
        _email = email;
        _users = users;
        _guestAccounts = guestAccounts;
        _inAppNotifications = inAppNotifications;
        _clock = clock;
        _logger = logger;
    }

    public async Task<ServiceCheckoutQuoteResponse> GetQuoteAsync(
        ServiceCheckoutQuoteRequest request,
        CancellationToken cancellationToken = default)
    {
        var service = await LoadActiveServiceAsync(request.ServiceId, cancellationToken);
        var pricing = await CalculatePricingAsync(service, request, cancellationToken);

        return MapQuote(service, pricing);
    }

    public async Task RequestCheckoutOtpAsync(string email, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(email))
            throw new InvalidOperationException("Email is required.");

        var normalized = email.Trim().ToLowerInvariant();
        var otp = _otp.GenerateOtp();
        await _otp.StoreOtpAsync(normalized, otp, OtpPurpose.Checkout, cancellationToken);

        var body = $"""
            <p>Your Ideal Weight Nutrition verification code is:</p>
            <p style="font-size:28px;font-weight:bold;letter-spacing:4px">{otp}</p>
            <p>This code expires in 10 minutes.</p>
            """;

        await _email.SendAsync(
            normalized,
            "Email verification code — Ideal Weight Nutrition",
            body,
            cancellationToken);
    }

    public Task<OtpVerificationResult> VerifyCheckoutOtpAsync(
        string email,
        string otp,
        CancellationToken cancellationToken = default) =>
        _otp.VerifyOtpAsync(email, otp, OtpPurpose.Checkout, cancellationToken);

    public async Task<PaymentMethodsResponse> GetPaymentMethodsAsync(
        double amountToPay,
        CancellationToken cancellationToken = default)
    {
        if (amountToPay <= 0.01)
            return new PaymentMethodsResponse { Methods = [] };

        var methods = await _payments.GetAvailableMethodsAsync(amountToPay, cancellationToken);
        return new PaymentMethodsResponse
        {
            Methods = methods.Methods
                .Where(m => !string.Equals(m.Id, PaymentMethods.Cod, StringComparison.OrdinalIgnoreCase))
                .ToList()
        };
    }

    public async Task<CreateServicePurchaseResponse> CreatePurchaseAsync(
        int serviceId,
        string? userId,
        CreateServicePurchaseRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidatePurchaseRequest(request);

        var service = await LoadActiveServiceAsync(serviceId, cancellationToken);
        var quoteRequest = new ServiceCheckoutQuoteRequest
        {
            ServiceId = serviceId,
            OfferId = request.OfferId,
            PromoCode = request.PromoCode,
            CustomAmount = request.CustomAmount
        };
        var pricing = await CalculatePricingAsync(service, quoteRequest, cancellationToken);

        var email = request.Email.Trim().ToLowerInvariant();
        var isGuest = string.IsNullOrEmpty(userId);

        if (isGuest)
        {
            var verified = await _otp.IsEmailVerifiedAsync(email, OtpPurpose.Checkout, cancellationToken);
            if (!verified)
            {
                if (string.IsNullOrWhiteSpace(request.Otp))
                    throw new InvalidOperationException("Please verify your email with the one-time code.");

                var otpResult = await _otp.VerifyOtpAsync(email, request.Otp, OtpPurpose.Checkout, cancellationToken);
                if (!otpResult.IsValid)
                    throw new InvalidOperationException(otpResult.Message ?? "Invalid verification code.");
            }
        }

        string? purchaseUserId = userId;
        var accountCreated = false;
        var accountLinked = false;

        if (isGuest)
        {
            var guestAccount = await _guestAccounts.ResolveOrCreateAsync(
                email,
                request.Name,
                request.PhoneNumber,
                streetAddress: null,
                city: null,
                state: null,
                postalCode: null,
                request.CreateAccountForGuest,
                cancellationToken);

            if (guestAccount.UserId is not null)
            {
                purchaseUserId = guestAccount.UserId;
                accountCreated = guestAccount.CreatedNewAccount;
                accountLinked = guestAccount.LinkedExistingAccount;
            }
        }

        var isFree = pricing.IsFree;
        string? paymentMethod = null;
        if (!isFree)
        {
            paymentMethod = NormalizePaymentMethod(request.PaymentMethod);
            if (string.IsNullOrWhiteSpace(paymentMethod))
                throw new InvalidOperationException("Please select a payment method.");
        }

        var purchase = new ServicePurchase
        {
            ServiceSubscriptionId = service.Id,
            ApplicationUserId = purchaseUserId,
            GuestEmail = string.IsNullOrEmpty(purchaseUserId) ? email : null,
            GuestName = string.IsNullOrEmpty(purchaseUserId) ? request.Name.Trim() : null,
            GuestPhone = string.IsNullOrEmpty(purchaseUserId) ? request.PhoneNumber.Trim() : null,
            TotalAmount = pricing.TotalAmount,
            AmountPaid = pricing.AmountToPay,
            DiscountAmount = pricing.DiscountAmount,
            ServiceOfferId = pricing.AppliedOfferId,
            PurchaseDate = _clock.Now,
            Status = "Active",
            PaymentStatus = isFree ? ServicePaymentApproved : PaymentStatuses.Pending,
            PaymentIntentId = isFree ? "FREE" : paymentMethod
        };

        _db.ServicePurchases.Add(purchase);
        await _db.SaveChangesAsync(cancellationToken);

        try
        {
            await _inAppNotifications.NotifyAdminsAsync(
                "New Service Purchase",
                $"Service purchase #{purchase.Id} for {service.Title} was created.",
                "ServiceSubscription",
                relatedId: purchase.Id,
                cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to create in-app notification for service purchase #{PurchaseId}", purchase.Id);
        }

        if (isFree)
        {
            return new CreateServicePurchaseResponse
            {
                PurchaseId = purchase.Id,
                PaymentStatus = purchase.PaymentStatus,
                AmountPaid = (double)purchase.AmountPaid,
                PaymentMethod = null,
                RequiresPaymentAction = false,
                IsPaid = true,
                AccountCreated = accountCreated,
                AccountLinked = accountLinked
            };
        }

        PaymentInitiationResult? paymentInit = null;
        try
        {
            paymentInit = await _payments.InitiateServicePurchaseAsync(
                purchase,
                service,
                request,
                cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Service payment initiation failed for purchase {PurchaseId}", purchase.Id);
            throw new InvalidOperationException(ex.Message);
        }

        return new CreateServicePurchaseResponse
        {
            PurchaseId = purchase.Id,
            PaymentStatus = purchase.PaymentStatus,
            AmountPaid = (double)purchase.AmountPaid,
            PaymentMethod = paymentMethod,
            RequiresPaymentAction = true,
            PaymentSessionId = paymentInit?.SessionId,
            PaymentRedirectUrl = paymentInit?.RedirectUrl,
            IsPaid = false,
            AccountCreated = accountCreated,
            AccountLinked = accountLinked
        };
    }

    public Task<CompleteServicePaymentResponse> CompletePaymentAsync(
        int purchaseId,
        CancellationToken cancellationToken = default) =>
        _payments.CompleteServicePurchaseAsync(purchaseId, cancellationToken);

    private async Task<ServiceSubscription> LoadActiveServiceAsync(
        int serviceId,
        CancellationToken cancellationToken)
    {
        var service = await _db.ServiceSubscriptions
            .AsNoTracking()
            .Include(s => s.Offers)
            .FirstOrDefaultAsync(s => s.Id == serviceId && s.IsActive, cancellationToken);

        return service ?? throw new InvalidOperationException("Service not found or not active.");
    }

    private async Task<ServicePricing> CalculatePricingAsync(
        ServiceSubscription service,
        ServiceCheckoutQuoteRequest request,
        CancellationToken cancellationToken)
    {
        var listPrice = service.Price;
        var discountAmount = 0m;
        int? appliedOfferId = null;
        string? appliedPromoCode = null;
        string? promoMessage = null;

        if (request.OfferId is > 0)
        {
            var now = _clock.Now;
            var offer = service.Offers.FirstOrDefault(o =>
                o.Id == request.OfferId
                && o.IsActive
                && o.StartDate <= now
                && o.EndDate >= now);

            if (offer is not null)
            {
                appliedOfferId = offer.Id;
                discountAmount += offer.DiscountType == ServiceDiscountType.Percentage
                    ? listPrice * (offer.DiscountValue / 100m)
                    : offer.DiscountValue;
            }
        }

        var totalAmount = listPrice - discountAmount;
        if (totalAmount < 0)
            totalAmount = 0;

        if (!string.IsNullOrWhiteSpace(request.PromoCode))
        {
            var promoResult = await TryApplyPromoAsync(
                request.PromoCode.Trim(),
                service.Id,
                totalAmount,
                cancellationToken);

            promoMessage = promoResult.Message;
            if (promoResult.Discount > 0)
            {
                discountAmount += promoResult.Discount;
                totalAmount -= promoResult.Discount;
                appliedPromoCode = promoResult.Code;
            }
        }

        if (totalAmount < 0)
            totalAmount = 0;

        var isFree = totalAmount <= 0.01m;
        decimal amountToPay = totalAmount;
        decimal? minPaymentAmount = null;

        if (!isFree
            && service.ServiceType == ServiceType.Offline
            && service.OfflinePaymentPercent is > 0)
        {
            minPaymentAmount = totalAmount * (service.OfflinePaymentPercent.Value / 100m);

            if (request.CustomAmount is > 0)
            {
                var custom = (decimal)request.CustomAmount.Value;
                if (custom < minPaymentAmount)
                {
                    throw new InvalidOperationException(
                        $"Amount must be at least AED {minPaymentAmount:0.##} ({service.OfflinePaymentPercent:0.##}% of total).");
                }

                if (custom > totalAmount)
                {
                    throw new InvalidOperationException(
                        $"Amount cannot exceed AED {totalAmount:0.##}.");
                }

                amountToPay = custom;
            }
            else
            {
                amountToPay = minPaymentAmount.Value;
            }
        }
        else if (isFree)
        {
            amountToPay = 0;
        }

        return new ServicePricing(
            listPrice,
            discountAmount,
            totalAmount,
            amountToPay,
            minPaymentAmount,
            isFree,
            appliedOfferId,
            appliedPromoCode,
            promoMessage);
    }

    private async Task<(decimal Discount, string? Code, string? Message)> TryApplyPromoAsync(
        string code,
        int serviceId,
        decimal amountAfterOffer,
        CancellationToken cancellationToken)
    {
        var promo = await _db.PromoCodes
            .AsNoTracking()
            .Include(p => p.ExcludedServiceSubscriptions)
            .FirstOrDefaultAsync(p => p.Code.ToLower() == code.ToLower(), cancellationToken);

        if (promo is null || !promo.IsActive)
            return (0, null, "Invalid or inactive promo code.");

        var now = _clock.Now;
        if (now < promo.StartDate)
            return (0, null, "This promo code is not valid yet.");
        if (now > promo.EndDate)
            return (0, null, "This promo code has expired.");

        if (promo.ExcludedServiceSubscriptions.Any(e => e.ServiceSubscriptionId == serviceId))
            return (0, null, "This promo code does not apply to this service.");

        if (promo.MinimumOrderAmount is > 0 && amountAfterOffer < promo.MinimumOrderAmount)
        {
            return (0, null, $"Minimum amount of AED {promo.MinimumOrderAmount:0.##} is required.");
        }

        decimal promoDiscount = promo.DiscountType == DiscountType.Percentage
            ? amountAfterOffer * (promo.DiscountValue / 100m)
            : promo.DiscountValue;

        if (promo.MaximumDiscountAmount is > 0 && promoDiscount > promo.MaximumDiscountAmount)
            promoDiscount = promo.MaximumDiscountAmount.Value;

        if (promoDiscount > amountAfterOffer)
            promoDiscount = amountAfterOffer;

        if (promoDiscount <= 0)
            return (0, null, "This promo code does not apply.");

        return (promoDiscount, promo.Code, "Promo code applied.");
    }

    private static ServiceCheckoutQuoteResponse MapQuote(ServiceSubscription service, ServicePricing pricing) =>
        new()
        {
            ServiceId = service.Id,
            ServiceTitle = service.Title,
            ServiceTitleAr = service.TitleAr,
            ListPrice = (double)pricing.ListPrice,
            DiscountAmount = (double)pricing.DiscountAmount,
            TotalAmount = (double)pricing.TotalAmount,
            AmountToPay = (double)pricing.AmountToPay,
            MinPaymentAmount = pricing.MinPaymentAmount.HasValue ? (double)pricing.MinPaymentAmount.Value : null,
            IsFree = pricing.IsFree,
            ServiceType = service.ServiceType.ToString(),
            AppliedOfferId = pricing.AppliedOfferId,
            AppliedPromoCode = pricing.AppliedPromoCode,
            PromoMessage = pricing.PromoMessage
        };

    private static void ValidatePurchaseRequest(CreateServicePurchaseRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            throw new InvalidOperationException("Name is required.");
        if (string.IsNullOrWhiteSpace(request.Email))
            throw new InvalidOperationException("Email is required.");
        if (string.IsNullOrWhiteSpace(request.PhoneNumber))
            throw new InvalidOperationException("Phone number is required.");
    }

    private static string? NormalizePaymentMethod(string? method)
    {
        if (string.IsNullOrWhiteSpace(method))
            return null;

        return method.Trim() switch
        {
            var m when m.Equals(PaymentMethods.Geidea, StringComparison.OrdinalIgnoreCase) => PaymentMethods.Geidea,
            var m when m.Equals(PaymentMethods.Tamara, StringComparison.OrdinalIgnoreCase) => PaymentMethods.Tamara,
            var m when m.Equals(PaymentMethods.Tabby, StringComparison.OrdinalIgnoreCase) => PaymentMethods.Tabby,
            _ => throw new InvalidOperationException("Unsupported payment method for services.")
        };
    }

    private sealed record ServicePricing(
        decimal ListPrice,
        decimal DiscountAmount,
        decimal TotalAmount,
        decimal AmountToPay,
        decimal? MinPaymentAmount,
        bool IsFree,
        int? AppliedOfferId,
        string? AppliedPromoCode,
        string? PromoMessage);
}
