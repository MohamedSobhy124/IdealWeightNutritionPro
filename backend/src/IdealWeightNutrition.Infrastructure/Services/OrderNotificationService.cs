using IdealWeightNutrition.Application.Abstractions;
using IdealWeightNutrition.Domain.Checkout;
using IdealWeightNutrition.Infrastructure.Persistence;
using IdealWeightNutrition.Utility;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using IdealWeightNutrition.Domain.Identity;
using LegacyOrderDetail = IdealWeightNutrition.Models.OrderDetail;
using LegacyOrderHeader = IdealWeightNutrition.Models.OrderHeader;
using LegacyProduct = IdealWeightNutrition.Models.Product;
using LegacyUser = IdealWeightNutrition.Models.ApplicationUser;

namespace IdealWeightNutrition.Infrastructure.Services;

internal sealed class OrderNotificationService : IOrderNotificationService
{
    private readonly AppDbContext _db;
    private readonly IEmailService _email;
    private readonly IInAppNotificationService _inApp;
    private readonly UserManager<ApplicationUser> _users;
    private readonly InvoiceService _invoices;
    private readonly IConfiguration _configuration;
    private readonly ILogger<OrderNotificationService> _logger;

    public OrderNotificationService(
        AppDbContext db,
        IEmailService email,
        IInAppNotificationService inApp,
        UserManager<ApplicationUser> users,
        IConfiguration configuration,
        ILogger<OrderNotificationService> logger)
    {
        _db = db;
        _email = email;
        _inApp = inApp;
        _users = users;
        _configuration = configuration;
        _logger = logger;
        _invoices = new InvoiceService(configuration);
    }

    public async Task SendOrderConfirmationAsync(int orderId, CancellationToken cancellationToken = default)
    {
        var order = await _db.OrderHeaders
            .AsNoTracking()
            .Include(o => o.Details)
            .FirstOrDefaultAsync(o => o.Id == orderId, cancellationToken);

        if (order is null || string.IsNullOrWhiteSpace(order.Email))
            return;

        var productIds = order.Details.Select(d => d.ProductId).Distinct().ToList();
        var products = await _db.Products.AsNoTracking()
            .Where(p => productIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, cancellationToken);

        LegacyUser? legacyUser = null;
        if (!string.IsNullOrEmpty(order.ApplicationUserId))
        {
            var user = await _users.Users.AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == order.ApplicationUserId, cancellationToken);
            if (user is not null)
            {
                legacyUser = new LegacyUser
                {
                    Id = user.Id,
                    Email = user.Email ?? order.Email,
                    UserName = user.UserName,
                    Name = user.Name ?? order.Name ?? string.Empty,
                    PhoneNumber = user.PhoneNumber
                };

                try
                {
                    await _inApp.NotifyUserAsync(
                        user.Id,
                        "Order Confirmed",
                        $"Your order #{order.Id} has been confirmed. Total: AED {order.OrderTotal:N2}",
                        "Order",
                        orderId: order.Id,
                        cancellationToken: cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to send in-app order confirmation for order #{OrderId}", orderId);
                }
            }
        }

        var legacyHeader = MapHeader(order);
        var legacyDetails = order.Details.Select(d => MapDetail(d, products)).ToList();
        var subject = $"Order Confirmation #{order.Id} - Ideal Weight";
        var body = BuildConfirmationEmailBody(order, legacyDetails);

        byte[]? pdf = null;
        try
        {
            pdf = _invoices.GenerateInvoicePdf(legacyHeader, legacyDetails, legacyUser);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to generate invoice PDF for order #{OrderId}", orderId);
        }

        var recipient = order.Email.Trim();
        if (pdf is { Length: > 0 })
        {
            await _email.SendWithAttachmentAsync(
                recipient,
                subject,
                body,
                pdf,
                $"Invoice-{order.Id}.pdf",
                cancellationToken);
        }
        else
        {
            await _email.SendAsync(recipient, subject, body, cancellationToken);
        }
    }

    public async Task SendOrderDeliveredAsync(int orderId, CancellationToken cancellationToken = default)
    {
        var order = await _db.OrderHeaders.AsNoTracking()
            .FirstOrDefaultAsync(o => o.Id == orderId, cancellationToken);

        if (order is null || string.IsNullOrWhiteSpace(order.Email))
            return;

        var subject = $"Order #{order.Id} Delivered - Ideal Weight";
        var body = $"""
            <p>Hi {System.Net.WebUtility.HtmlEncode(order.Name)},</p>
            <p>Your order <strong>#{order.Id}</strong> has been delivered. Thank you for shopping with Ideal Weight Nutrition.</p>
            """;

        await _email.SendAsync(order.Email.Trim(), subject, body, cancellationToken);

        if (!string.IsNullOrEmpty(order.ApplicationUserId))
        {
            try
            {
                await _inApp.NotifyUserAsync(
                    order.ApplicationUserId,
                    "Order Delivered",
                    $"Your order #{order.Id} has been delivered.",
                    "Order",
                    orderId: order.Id,
                    cancellationToken: cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to send delivery in-app notification for order #{OrderId}", orderId);
            }
        }
    }

    public async Task<byte[]?> GenerateInvoicePdfAsync(
        int orderId,
        string? userId,
        string? guestEmail,
        CancellationToken cancellationToken = default)
    {
        var order = await _db.OrderHeaders
            .AsNoTracking()
            .Include(o => o.Details)
            .FirstOrDefaultAsync(o => o.Id == orderId, cancellationToken);

        if (order is null)
            return null;

        if (!await CanAccessOrderAsync(order, userId, guestEmail, cancellationToken))
            return null;

        var productIds = order.Details.Select(d => d.ProductId).Distinct().ToList();
        var products = await _db.Products.AsNoTracking()
            .Where(p => productIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, cancellationToken);

        LegacyUser? legacyUser = null;
        if (!string.IsNullOrEmpty(order.ApplicationUserId))
        {
            var user = await _users.Users.AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == order.ApplicationUserId, cancellationToken);
            if (user is not null)
            {
                legacyUser = new LegacyUser
                {
                    Id = user.Id,
                    Email = user.Email,
                    Name = user.Name ?? order.Name ?? string.Empty
                };
            }
        }

        var legacyHeader = MapHeader(order);
        var legacyDetails = order.Details.Select(d => MapDetail(d, products)).ToList();

        try
        {
            return _invoices.GenerateInvoicePdf(legacyHeader, legacyDetails, legacyUser);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to generate invoice PDF for order #{OrderId}", orderId);
            return null;
        }
    }

    private async Task<bool> CanAccessOrderAsync(
        OrderHeader order,
        string? userId,
        string? guestEmail,
        CancellationToken cancellationToken)
    {
        if (!string.IsNullOrEmpty(userId) && order.ApplicationUserId == userId)
            return true;

        if (string.IsNullOrWhiteSpace(guestEmail))
            return false;

        var normalizedEmail = guestEmail.Trim().ToLowerInvariant();
        if (order.IsGuestOrder)
            return order.Email?.Trim().ToLowerInvariant() == normalizedEmail;

        if (string.IsNullOrEmpty(order.ApplicationUserId))
            return order.Email?.Trim().ToLowerInvariant() == normalizedEmail;

        var user = await _users.Users.AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == order.ApplicationUserId, cancellationToken);

        return user?.Email?.Trim().ToLowerInvariant() == normalizedEmail;
    }

    private static LegacyOrderHeader MapHeader(OrderHeader order) => new()
    {
        Id = order.Id,
        ApplicationUserId = order.ApplicationUserId,
        Email = order.Email,
        IsGuestOrder = order.IsGuestOrder,
        OrderDate = order.OrderDate,
        ShippingDate = order.ShippingDate,
        OrderTotal = order.OrderTotal,
        OrderStatus = order.OrderStatus,
        PaymentStatus = order.PaymentStatus,
        TrackingNumber = order.TrackingNumber,
        Carrier = order.Carrier,
        PaymentDate = order.PaymentDate,
        PaymentDueDate = order.PaymentDueDate,
        SessionId = order.SessionId,
        PaymentIntentId = order.PaymentIntentId,
        PaymentMethod = order.PaymentMethod,
        PhoneNumber = order.PhoneNumber,
        StreetAddress = order.StreetAddress,
        City = order.City,
        Area = order.Area,
        State = order.State,
        PostalCode = order.PostalCode,
        Name = order.Name,
        PromoCodeId = order.PromoCodeId,
        PromoCodeText = order.PromoCodeText,
        DiscountAmount = order.DiscountAmount,
        OrderSubtotal = order.OrderSubtotal
    };

    private static LegacyOrderDetail MapDetail(
        OrderDetail detail,
        IReadOnlyDictionary<int, Domain.Catalogue.Product> products)
    {
        products.TryGetValue(detail.ProductId, out var product);
        return new LegacyOrderDetail
        {
            Id = detail.Id,
            OrderHeaderId = detail.OrderHeaderId,
            ProductId = detail.ProductId,
            Count = detail.Count,
            Price = detail.Price,
            FlashSaleItemId = detail.FlashSaleItemId,
            ProductVariantId = detail.ProductVariantId,
            ComboOfferId = detail.ComboOfferId,
            Product = product is null
                ? null!
                : new LegacyProduct
                {
                    Id = product.Id,
                    Title = product.Title,
                    TitleAr = product.TitleAr,
                    Price = product.Price,
                    ListPrice = product.ListPrice
                }
        };
    }

    private string BuildConfirmationEmailBody(OrderHeader order, IReadOnlyList<LegacyOrderDetail> items)
    {
        var siteName = _configuration["SiteSettings:Business:Name"] ?? "Ideal Weight Nutrition";
        var rows = string.Join(
            string.Empty,
            items.Select(i =>
                $"<tr><td>{System.Net.WebUtility.HtmlEncode(i.Product?.Title ?? $"Product #{i.ProductId}")}</td>" +
                $"<td>{i.Count}</td><td>AED {i.Price * i.Count:N2}</td></tr>"));

        return $"""
            <p>Hi {System.Net.WebUtility.HtmlEncode(order.Name)},</p>
            <p>Thank you for your order at {System.Net.WebUtility.HtmlEncode(siteName)}.</p>
            <p><strong>Order #{order.Id}</strong> · {order.OrderDate:dd MMM yyyy}</p>
            <table border="1" cellpadding="8" cellspacing="0" style="border-collapse:collapse;">
              <thead><tr><th>Item</th><th>Qty</th><th>Total</th></tr></thead>
              <tbody>{rows}</tbody>
            </table>
            <p><strong>Order total: AED {order.OrderTotal:N2}</strong></p>
            <p>Your invoice is attached to this email.</p>
            """;
    }
}
