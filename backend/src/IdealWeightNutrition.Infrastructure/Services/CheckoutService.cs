using IdealWeightNutrition.Application.Abstractions;
using IdealWeightNutrition.Contracts.Checkout;
using IdealWeightNutrition.Domain.Catalogue;
using IdealWeightNutrition.Domain.Checkout;
using IdealWeightNutrition.Domain.Constants;
using IdealWeightNutrition.Domain.Identity;
using IdealWeightNutrition.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace IdealWeightNutrition.Infrastructure.Services;

internal sealed class CheckoutService : ICheckoutService
{
    private readonly AppDbContext _db;
    private readonly ICartService _cart;
    private readonly IPromoCodeService _promoCodes;
    private readonly IInventoryService _inventory;
    private readonly IOtpService _otp;
    private readonly IEmailService _email;
    private readonly UserManager<ApplicationUser> _users;
    private readonly IDateTimeProvider _clock;
    private readonly IPaymentService _payments;
    private readonly IGuestAccountService _guestAccounts;
    private readonly IInAppNotificationService _inAppNotifications;
    private readonly IOrderNotificationService _orderNotifications;
    private readonly ILogger<CheckoutService> _logger;

    public CheckoutService(
        AppDbContext db,
        ICartService cart,
        IPromoCodeService promoCodes,
        IInventoryService inventory,
        IOtpService otp,
        IEmailService email,
        UserManager<ApplicationUser> users,
        IDateTimeProvider clock,
        IPaymentService payments,
        IGuestAccountService guestAccounts,
        IInAppNotificationService inAppNotifications,
        IOrderNotificationService orderNotifications,
        ILogger<CheckoutService> logger)
    {
        _db = db;
        _cart = cart;
        _promoCodes = promoCodes;
        _inventory = inventory;
        _otp = otp;
        _email = email;
        _users = users;
        _clock = clock;
        _payments = payments;
        _guestAccounts = guestAccounts;
        _inAppNotifications = inAppNotifications;
        _orderNotifications = orderNotifications;
        _logger = logger;
    }

    public async Task<IReadOnlyList<CityDto>> ListCitiesAsync(CancellationToken cancellationToken = default) =>
        await _db.Cities
            .AsNoTracking()
            .Where(c => c.IsActive)
            .OrderBy(c => c.DisplayOrder)
            .ThenBy(c => c.Name)
            .Select(c => new CityDto
            {
                Id = c.Id,
                Name = c.Name,
                NameAr = c.NameAr,
                Emirate = c.Emirate,
                DeliveryCharge = c.DeliveryCharge
            })
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<RemoteAreaDto>> ListRemoteAreasAsync(
        int cityId,
        CancellationToken cancellationToken = default) =>
        await _db.RemoteAreas
            .AsNoTracking()
            .Where(r => r.CityId == cityId && r.IsActive)
            .OrderBy(r => r.DisplayOrder)
            .ThenBy(r => r.Name)
            .Select(r => new RemoteAreaDto
            {
                Id = r.Id,
                CityId = r.CityId,
                Name = r.Name,
                NameAr = r.NameAr,
                DeliveryCharge = r.DeliveryCharge
            })
            .ToListAsync(cancellationToken);

    public async Task<ShippingQuoteResponse> GetShippingQuoteAsync(
        string? userId,
        string? guestCartId,
        ShippingQuoteRequest request,
        CancellationToken cancellationToken = default)
    {
        var cart = await RequireNonEmptyCartAsync(userId, guestCartId, cancellationToken);
        var (city, areaName, shipping) = await ResolveShippingAsync(cart.Items, cart.Subtotal, request, cancellationToken);
        var discount = cart.Discount;
        var merchandiseTotal = Math.Max(0, cart.Subtotal - discount);

        return new ShippingQuoteResponse
        {
            Subtotal = cart.Subtotal,
            Discount = discount,
            Shipping = shipping,
            Total = merchandiseTotal + shipping,
            CityName = city.Name,
            AreaName = areaName
        };
    }

    public async Task RequestCheckoutOtpAsync(string email, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(email))
            throw new InvalidOperationException("Email is required.");

        var normalized = email.Trim().ToLowerInvariant();
        if (!IsValidEmail(normalized))
            throw new InvalidOperationException("Please enter a valid email address.");

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

        if (_logger.IsEnabled(LogLevel.Debug))
            _logger.LogDebug("Checkout OTP for {Email}: {Otp}", normalized, otp);
    }

    public Task<OtpVerificationResult> VerifyCheckoutOtpAsync(
        string email,
        string otp,
        CancellationToken cancellationToken = default) =>
        _otp.VerifyOtpAsync(email, otp, OtpPurpose.Checkout, cancellationToken);

    public Task<PaymentMethodsResponse> GetPaymentMethodsAsync(
        double orderTotal,
        CancellationToken cancellationToken = default) =>
        _payments.GetAvailableMethodsAsync(orderTotal, cancellationToken);

    public Task<CompletePaymentResponse> CompletePaymentAsync(
        int orderId,
        string? userId,
        string? guestCartId,
        CancellationToken cancellationToken = default) =>
        _payments.CompleteAsync(orderId, userId, guestCartId, cancellationToken);

    public async Task<CreateOrderResponse> CreateOrderAsync(
        string? userId,
        string? guestCartId,
        CreateOrderRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidateOrderRequest(request);

        var cart = await RequireNonEmptyCartAsync(userId, guestCartId, cancellationToken);
        await _inventory.EnsureStockAvailableAsync(cart.Items, cancellationToken);

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

        var city = await _db.Cities
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == request.CityId && c.IsActive, cancellationToken)
            ?? throw new InvalidOperationException("Selected city is not available.");

        string? areaName = null;
        if (request.RemoteAreaId is > 0)
        {
            var remoteArea = await _db.RemoteAreas
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    r => r.Id == request.RemoteAreaId && r.CityId == city.Id && r.IsActive,
                    cancellationToken);

            if (remoteArea is null)
                throw new InvalidOperationException("Selected area is not available.");

            areaName = remoteArea.Name;
        }
        else
        {
            var hasRemoteAreas = await _db.RemoteAreas
                .AnyAsync(r => r.CityId == city.Id && r.IsActive, cancellationToken);
            if (hasRemoteAreas)
                throw new InvalidOperationException("Please select a delivery area.");
        }

        var shipping = await CalculateShippingAsync(cart.Items, cart.Subtotal, city, request.RemoteAreaId, cancellationToken);
        var subtotal = cart.Subtotal;
        var discount = cart.Discount;
        var merchandiseTotal = Math.Max(0, subtotal - discount);
        var total = merchandiseTotal + shipping;
        var now = _clock.Now;

        string? orderUserId = userId;
        var isGuestOrder = isGuest;
        var accountCreated = false;
        var accountLinked = false;

        if (isGuest)
        {
            var guestAccount = await _guestAccounts.ResolveOrCreateAsync(
                email,
                request.Name,
                request.PhoneNumber,
                request.StreetAddress,
                city.Name,
                request.State,
                request.PostalCode,
                request.CreateAccountForGuest,
                cancellationToken);

            if (guestAccount.UserId is not null)
            {
                orderUserId = guestAccount.UserId;
                isGuestOrder = false;
                accountCreated = guestAccount.CreatedNewAccount;
                accountLinked = guestAccount.LinkedExistingAccount;
            }
        }

        var (orderStatus, paymentStatus) = await ResolveStatusesAsync(orderUserId, cancellationToken);
        var paymentMethod = NormalizePaymentMethod(request.PaymentMethod);
        var isOnlinePayment = IsOnlinePayment(paymentMethod);

        if (isOnlinePayment)
        {
            var user = string.IsNullOrEmpty(orderUserId)
                ? null
                : await _users.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == orderUserId, cancellationToken);
            if (user?.CompanyId is > 0)
                throw new InvalidOperationException("Online payment is not available for company accounts.");

            orderStatus = OrderStatuses.Pending;
            paymentStatus = PaymentStatuses.Pending;
        }

        await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);

        var order = new OrderHeader
        {
            ApplicationUserId = orderUserId,
            Email = email,
            IsGuestOrder = isGuestOrder,
            OrderDate = now,
            ShippingDate = DateTime.MinValue,
            OrderSubtotal = subtotal,
            DiscountAmount = discount > 0 ? discount : null,
            PromoCodeId = cart.AppliedPromo?.Id,
            PromoCodeText = cart.AppliedPromo?.Code,
            OrderTotal = total,
            OrderStatus = orderStatus,
            PaymentStatus = paymentStatus,
            PaymentMethod = paymentMethod,
            PaymentDate = DateTime.MinValue,
            PaymentDueDate = DateTime.MinValue,
            PhoneNumber = request.PhoneNumber.Trim(),
            StreetAddress = request.StreetAddress.Trim(),
            City = city.Name,
            Area = areaName,
            State = string.IsNullOrWhiteSpace(request.State) ? "UAE" : request.State.Trim(),
            PostalCode = string.IsNullOrWhiteSpace(request.PostalCode) ? "00000" : request.PostalCode.Trim(),
            Name = request.Name.Trim()
        };

        _db.OrderHeaders.Add(order);
        await _db.SaveChangesAsync(cancellationToken);

        var orderProductIds = cart.Items
            .Where(i => i.ComboOfferId is null or 0)
            .Select(i => i.ProductId)
            .Distinct()
            .ToList();
        var orderProducts = orderProductIds.Count == 0
            ? new Dictionary<int, Product>()
            : await _db.Products
                .AsNoTracking()
                .Include(p => p.Variants.Where(v => !v.IsDeleted))
                .Where(p => orderProductIds.Contains(p.Id) && !p.IsDeleted)
                .ToDictionaryAsync(p => p.Id, cancellationToken);

        foreach (var item in cart.Items)
        {
            var variantId = item.ProductVariantId;
            if (variantId is not > 0
                && orderProducts.TryGetValue(item.ProductId, out var product)
                && product.ProductType == ProductType.Variable)
            {
                variantId = product.Variants
                    .Where(v => !v.IsDeleted && v.StockQuantity > 0)
                    .MinBy(v => v.Price)?.Id;
            }

            _db.OrderDetails.Add(new OrderDetail
            {
                OrderHeaderId = order.Id,
                ProductId = item.ProductId,
                Count = item.Quantity,
                Price = item.UnitPrice,
                ProductVariantId = variantId,
                FlashSaleItemId = item.FlashSaleItemId,
                ComboOfferId = item.ComboOfferId
            });
        }

        await _db.SaveChangesAsync(cancellationToken);

        if (!isOnlinePayment)
        {
            await _inventory.DeductStockForOrderAsync(order.Id, cancellationToken);

            if (order.PromoCodeId is > 0 && !string.IsNullOrEmpty(orderUserId))
            {
                await _promoCodes.RecordUsageAsync(order.PromoCodeId.Value, orderUserId, order.Id, cancellationToken);
            }
        }

        await transaction.CommitAsync(cancellationToken);

        try
        {
            await _inAppNotifications.NotifyAdminsAsync(
                "New Order Received",
                $"New order #{order.Id} has been placed by {order.Name}. Total: AED {order.OrderTotal:N2}",
                "Order",
                order.Id,
                cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to create in-app notification for order #{OrderId}", order.Id);
        }

        PaymentInitiationResult? paymentInit = null;
        if (isOnlinePayment)
        {
            try
            {
                paymentInit = await _payments.InitiateAsync(order, cart.Items, request, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Payment initiation failed for order {OrderId}", order.Id);
                throw new InvalidOperationException(ex.Message);
            }
        }
        else
        {
            await _cart.ClearCartAsync(userId, guestCartId, cancellationToken);
            try
            {
                await _orderNotifications.SendOrderConfirmationAsync(order.Id, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to send order confirmation for order #{OrderId}", order.Id);
            }
        }

        return new CreateOrderResponse
        {
            OrderId = order.Id,
            OrderStatus = order.OrderStatus ?? OrderStatuses.Pending,
            PaymentStatus = order.PaymentStatus ?? PaymentStatuses.Pending,
            OrderTotal = order.OrderTotal,
            PaymentMethod = paymentMethod,
            RequiresPaymentAction = isOnlinePayment,
            PaymentSessionId = paymentInit?.SessionId,
            PaymentRedirectUrl = paymentInit?.RedirectUrl,
            AccountCreated = accountCreated,
            AccountLinked = accountLinked
        };
    }

    private static bool IsOnlinePayment(string paymentMethod) =>
        paymentMethod is PaymentMethods.Geidea or PaymentMethods.Tamara or PaymentMethods.Tabby;

    private static string NormalizePaymentMethod(string method)
    {
        var value = method.Trim();
        if (value.Equals("cod", StringComparison.OrdinalIgnoreCase)
            || value.Equals("cash on delivery", StringComparison.OrdinalIgnoreCase))
            return PaymentMethods.Cod;
        if (value.Equals("geidea", StringComparison.OrdinalIgnoreCase)
            || value.Equals("card", StringComparison.OrdinalIgnoreCase))
            return PaymentMethods.Geidea;
        if (value.Equals("tamara", StringComparison.OrdinalIgnoreCase))
            return PaymentMethods.Tamara;
        if (value.Equals("tabby", StringComparison.OrdinalIgnoreCase)
            || value.Equals("tappy", StringComparison.OrdinalIgnoreCase))
            return PaymentMethods.Tabby;
        return value;
    }

    private async Task<(string orderStatus, string paymentStatus)> ResolveStatusesAsync(
        string? userId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(userId))
            return (OrderStatuses.Pending, PaymentStatuses.Pending);

        var user = await _users.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

        if (user?.CompanyId is > 0)
            return (OrderStatuses.Paid, PaymentStatuses.DelayedPayment);

        return (OrderStatuses.Pending, PaymentStatuses.Pending);
    }

    private static void ValidateOrderRequest(CreateOrderRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            throw new InvalidOperationException("Name is required.");
        if (string.IsNullOrWhiteSpace(request.Email))
            throw new InvalidOperationException("Email is required.");
        if (!IsValidEmail(request.Email.Trim().ToLowerInvariant()))
            throw new InvalidOperationException("Please enter a valid email address.");
        if (string.IsNullOrWhiteSpace(request.PhoneNumber))
            throw new InvalidOperationException("Phone number is required.");
        if (string.IsNullOrWhiteSpace(request.StreetAddress))
            throw new InvalidOperationException("Street address is required.");
        if (request.CityId <= 0)
            throw new InvalidOperationException("City is required.");
        if (string.IsNullOrWhiteSpace(request.PaymentMethod))
            throw new InvalidOperationException("Payment method is required.");
    }

    private async Task<Contracts.Cart.CartResponse> RequireNonEmptyCartAsync(
        string? userId,
        string? guestCartId,
        CancellationToken cancellationToken)
    {
        var cart = await _cart.GetCartAsync(userId, guestCartId, cancellationToken);
        if (cart.Items.Count == 0)
            throw new InvalidOperationException("Your cart is empty.");

        return cart;
    }

    private async Task<(City city, string? areaName, double shipping)> ResolveShippingAsync(
        IReadOnlyList<Contracts.Cart.CartItemDto> items,
        double subtotal,
        ShippingQuoteRequest request,
        CancellationToken cancellationToken)
    {
        var city = await _db.Cities
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == request.CityId && c.IsActive, cancellationToken)
            ?? throw new InvalidOperationException("Selected city is not available.");

        string? areaName = null;
        if (request.RemoteAreaId is > 0)
        {
            var area = await _db.RemoteAreas
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    r => r.Id == request.RemoteAreaId && r.CityId == city.Id && r.IsActive,
                    cancellationToken);

            if (area is not null)
                areaName = area.Name;
        }

        var shipping = await CalculateShippingAsync(items, subtotal, city, request.RemoteAreaId, cancellationToken);
        return (city, areaName, shipping);
    }

    private async Task<double> CalculateShippingAsync(
        IReadOnlyList<Contracts.Cart.CartItemDto> items,
        double subtotal,
        City city,
        int? remoteAreaId,
        CancellationToken cancellationToken)
    {
        double deliveryCharge = city.DeliveryCharge;

        if (remoteAreaId is > 0)
        {
            var remoteArea = await _db.RemoteAreas
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    r => r.Id == remoteAreaId && r.CityId == city.Id && r.IsActive,
                    cancellationToken);

            if (remoteArea is not null)
                deliveryCharge = remoteArea.DeliveryCharge;
        }

        if (await QualifiesForFreeDeliveryAsync(items, subtotal, cancellationToken))
            deliveryCharge = 0;

        return deliveryCharge;
    }

    private async Task<bool> QualifiesForFreeDeliveryAsync(
        IReadOnlyList<Contracts.Cart.CartItemDto> items,
        double subtotal,
        CancellationToken cancellationToken)
    {
        if (items.Count == 0)
            return false;

        var productIds = items.Select(i => i.ProductId).Distinct().ToList();
        var products = await _db.Products
            .AsNoTracking()
            .Where(p => productIds.Contains(p.Id))
            .Select(p => new { p.AllowFreeDelivery, p.FreeDeliveryMinimumAmount })
            .ToListAsync(cancellationToken);

        if (products.Count != productIds.Count || !products.All(p => p.AllowFreeDelivery))
            return false;

        var maxMinimum = products.Max(p => p.FreeDeliveryMinimumAmount);
        return subtotal >= maxMinimum;
    }

    private static bool IsValidEmail(string email) =>
        email.Contains('@') && email.Contains('.') && email.Length >= 5;
}
