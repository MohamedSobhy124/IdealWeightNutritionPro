using System.Net;
using System.Text;
using IdealWeightNutrition.Application.Abstractions;
using IdealWeightNutrition.Domain.Catalogue;
using IdealWeightNutrition.Domain.Constants;
using IdealWeightNutrition.Domain.Time;
using IdealWeightNutrition.Infrastructure.Options;
using IdealWeightNutrition.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace IdealWeightNutrition.Infrastructure.Services;

internal sealed class AdminNotificationService : IAdminNotificationService
{
    private readonly AppDbContext _db;
    private readonly IEmailService _email;
    private readonly StockAlertOptions _stockAlerts;
    private readonly AppUrlOptions _appUrls;
    private readonly ILogger<AdminNotificationService> _logger;
    private readonly IInAppNotificationService _inAppNotifications;

    public AdminNotificationService(
        AppDbContext db,
        IEmailService email,
        IOptions<StockAlertOptions> stockAlerts,
        IOptions<AppUrlOptions> appUrls,
        IInAppNotificationService inAppNotifications,
        ILogger<AdminNotificationService> logger)
    {
        _db = db;
        _email = email;
        _stockAlerts = stockAlerts.Value;
        _appUrls = appUrls.Value;
        _inAppNotifications = inAppNotifications;
        _logger = logger;
    }

    public async Task NotifyNewReturnRequestAsync(int returnRequestId, CancellationToken cancellationToken = default)
    {
        try
        {
            var recipients = await ResolveAdminRecipientsAsync(cancellationToken);
            if (recipients.Count == 0)
            {
                _logger.LogDebug("No admin recipients configured for return request #{ReturnId}", returnRequestId);
                return;
            }

            var returnRequest = await _db.ReturnRequests
                .AsNoTracking()
                .Include(r => r.Items)
                .FirstOrDefaultAsync(r => r.Id == returnRequestId, cancellationToken);

            if (returnRequest is null)
                return;

            var order = await _db.OrderHeaders
                .AsNoTracking()
                .FirstOrDefaultAsync(o => o.Id == returnRequest.OrderHeaderId, cancellationToken);

            if (order is null)
                return;

            var detailIds = returnRequest.Items.Select(i => i.OrderDetailId).ToList();
            var details = await _db.OrderDetails
                .AsNoTracking()
                .Where(d => detailIds.Contains(d.Id))
                .ToDictionaryAsync(d => d.Id, cancellationToken);

            var productIds = details.Values.Select(d => d.ProductId).Distinct().ToList();
            var products = await _db.Products
                .AsNoTracking()
                .Where(p => productIds.Contains(p.Id))
                .ToDictionaryAsync(p => p.Id, cancellationToken);

            var itemsRows = string.Join(
                "",
                returnRequest.Items.Select(item =>
                {
                    details.TryGetValue(item.OrderDetailId, out var detail);
                    var title = detail is not null && products.TryGetValue(detail.ProductId, out var product)
                        ? product.Title
                        : "Unknown product";
                    var encodedTitle = WebUtility.HtmlEncode(title);
                    var condition = WebUtility.HtmlEncode(item.ItemCondition ?? "N/A");
                    return $"""
                        <tr>
                          <td style="padding:8px;border-bottom:1px solid #e5e7eb;">{encodedTitle}</td>
                          <td style="padding:8px;border-bottom:1px solid #e5e7eb;text-align:center;">{item.Quantity}</td>
                          <td style="padding:8px;border-bottom:1px solid #e5e7eb;text-align:right;">AED {item.ReturnPrice * item.Quantity:N2}</td>
                          <td style="padding:8px;border-bottom:1px solid #e5e7eb;">{condition}</td>
                        </tr>
                        """;
                }));

            var adminUrl = $"{_appUrls.FrontendBaseUrl.TrimEnd('/')}/admin/returns/{returnRequest.Id}";
            var customerEmail = WebUtility.HtmlEncode(returnRequest.Email ?? order.Email ?? "—");
            var customerName = WebUtility.HtmlEncode(order.Name);
            var reason = WebUtility.HtmlEncode(returnRequest.Reason);
            var refund = returnRequest.RefundAmount?.ToString("N2") ?? "—";

            var body = $"""
                <p>A new return request was submitted.</p>
                <p><strong>Return #{returnRequest.Id}</strong> · Order #{order.Id}</p>
                <ul>
                  <li>Customer: {customerName}</li>
                  <li>Email: {customerEmail}</li>
                  <li>Reason: {reason}</li>
                  <li>Estimated refund: AED {refund}</li>
                </ul>
                <table style="width:100%;border-collapse:collapse;margin:16px 0;">
                  <thead>
                    <tr>
                      <th style="text-align:left;padding:8px;border-bottom:2px solid #e5e7eb;">Product</th>
                      <th style="padding:8px;border-bottom:2px solid #e5e7eb;">Qty</th>
                      <th style="text-align:right;padding:8px;border-bottom:2px solid #e5e7eb;">Amount</th>
                      <th style="text-align:left;padding:8px;border-bottom:2px solid #e5e7eb;">Condition</th>
                    </tr>
                  </thead>
                  <tbody>{itemsRows}</tbody>
                </table>
                <p><a href="{adminUrl}">Review in admin</a></p>
                """;

            var subject = $"New return request #{returnRequest.Id} — order #{order.Id}";
            await SendToRecipientsAsync(recipients, subject, body, cancellationToken);

            await _inAppNotifications.NotifyAdminsAsync(
                "New Return Request",
                $"Return request #{returnRequest.Id} has been submitted for order #{order.Id}",
                "ReturnRequest",
                order.Id,
                returnRequest.Id,
                cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to notify admins about return request #{ReturnId}", returnRequestId);
        }
    }

    public async Task CheckProductStockLevelsAsync(int productId, CancellationToken cancellationToken = default)
    {
        try
        {
            var product = await _db.Products.AsNoTracking().FirstOrDefaultAsync(p => p.Id == productId, cancellationToken);
            if (product is null || product.IsDeleted)
                return;

            var isOutOfStock = product.StockQuantity == 0;
            var isLowStock = product.StockQuantity > 0 && product.StockQuantity <= product.MinimumStockAlert;
            if (!isOutOfStock && !isLowStock)
                return;

            await SendStockAlertAsync(
                product.Title,
                product.Id,
                product.StockQuantity,
                product.MinimumStockAlert,
                isOutOfStock,
                cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to check stock levels for product #{ProductId}", productId);
        }
    }

    public async Task CheckVariantStockLevelsAsync(int variantId, CancellationToken cancellationToken = default)
    {
        try
        {
            var variant = await _db.Set<ProductVariant>()
                .AsNoTracking()
                .FirstOrDefaultAsync(v => v.Id == variantId && !v.IsDeleted, cancellationToken);

            if (variant is null)
                return;

            var product = await _db.Products.AsNoTracking().FirstOrDefaultAsync(p => p.Id == variant.ProductId, cancellationToken);
            if (product is null || product.IsDeleted)
                return;

            var isOutOfStock = variant.StockQuantity == 0;
            var isLowStock = variant.StockQuantity > 0 && variant.StockQuantity <= variant.MinimumStockAlert;
            if (!isOutOfStock && !isLowStock)
                return;

            var label = string.IsNullOrWhiteSpace(variant.Sku)
                ? $"{product.Title} (variant #{variant.Id})"
                : $"{product.Title} — {variant.Sku}";

            await SendStockAlertAsync(
                label,
                product.Id,
                variant.StockQuantity,
                variant.MinimumStockAlert,
                isOutOfStock,
                cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to check stock levels for variant #{VariantId}", variantId);
        }
    }

    public async Task NotifyStockNotificationRequestAsync(
        int productId,
        string customerEmail,
        string? phoneNumber,
        int? variantId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var product = await _db.Products.AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == productId, cancellationToken);
            if (product is null)
                return;

            var recipients = await ResolveAdminRecipientsAsync(cancellationToken);
            if (recipients.Count == 0)
                return;

            var variantInfo = variantId.HasValue ? $" (variant #{variantId.Value})" : string.Empty;
            var encodedProduct = WebUtility.HtmlEncode(product.Title);
            var encodedEmail = WebUtility.HtmlEncode(customerEmail);
            var encodedPhone = WebUtility.HtmlEncode(phoneNumber ?? "Not provided");

            var body = $"""
                <p>A customer requested to be notified when a product is back in stock.</p>
                <ul>
                  <li><strong>Product:</strong> {encodedProduct}{variantInfo}</li>
                  <li><strong>Email:</strong> {encodedEmail}</li>
                  <li><strong>Phone:</strong> {encodedPhone}</li>
                </ul>
                """;

            var subject = $"Stock notify request — {product.Title}";
            await SendToRecipientsAsync(recipients, subject, body, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to notify admins about stock notification for product #{ProductId}", productId);
        }
    }

    public async Task SendLowStockDigestAsync(CancellationToken cancellationToken = default)
    {
        if (!_stockAlerts.DailyDigestEnabled)
            return;

        try
        {
            var recipients = await ResolveAdminRecipientsAsync(cancellationToken);
            if (recipients.Count == 0)
            {
                _logger.LogDebug("No admin recipients configured for low stock digest.");
                return;
            }

            var simpleRows = await _db.Products.AsNoTracking()
                .Where(p => !p.IsDeleted && p.ProductType == ProductType.Simple)
                .Where(p => p.StockQuantity == 0 || (p.StockQuantity > 0 && p.StockQuantity <= p.MinimumStockAlert))
                .Select(p => new StockDigestRow(
                    p.Id,
                    p.Title,
                    null,
                    p.StockQuantity,
                    p.MinimumStockAlert,
                    p.StockQuantity == 0))
                .ToListAsync(cancellationToken);

            var variantRows = await _db.Set<ProductVariant>().AsNoTracking()
                .Where(v => !v.IsDeleted)
                .Where(v => v.StockQuantity == 0 || (v.StockQuantity > 0 && v.StockQuantity <= v.MinimumStockAlert))
                .Join(
                    _db.Products.AsNoTracking().Where(p => !p.IsDeleted),
                    v => v.ProductId,
                    p => p.Id,
                    (v, p) => new StockDigestRow(
                        p.Id,
                        p.Title,
                        v.Sku,
                        v.StockQuantity,
                        v.MinimumStockAlert,
                        v.StockQuantity == 0))
                .ToListAsync(cancellationToken);

            var rows = simpleRows
                .Concat(variantRows)
                .OrderBy(r => r.IsOutOfStock ? 0 : 1)
                .ThenBy(r => r.Stock)
                .ThenBy(r => r.Title)
                .ToList();

            if (rows.Count == 0)
            {
                _logger.LogInformation("Low stock digest: no products or variants below threshold.");
                return;
            }

            var csv = BuildStockDigestCsv(rows);
            var csvBytes = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(csv)).ToArray();
            var outOfStock = rows.Count(r => r.IsOutOfStock);
            var lowStock = rows.Count - outOfStock;
            var subject = $"Daily stock digest — {outOfStock} out of stock, {lowStock} low";
            var preview = string.Join(
                "<br/>",
                rows.Take(12).Select(r =>
                    $"{WebUtility.HtmlEncode(r.Title)}{(r.Sku is null ? "" : $" ({WebUtility.HtmlEncode(r.Sku)})")} — {(r.IsOutOfStock ? "OUT" : $"{r.Stock} left")}"));

            var body = $"""
                <p><strong>{rows.Count}</strong> product line(s) need attention today.</p>
                <p>Out of stock: <strong>{outOfStock}</strong> · Low stock: <strong>{lowStock}</strong></p>
                <p>{preview}</p>
                <p>See the attached CSV for the full list.</p>
                """;

            foreach (var recipient in recipients)
            {
                await _email.SendWithAttachmentAsync(
                    recipient,
                    subject,
                    body,
                    csvBytes,
                    $"Low-Stock-Digest-{UaeDateTime.Now:yyyy-MM-dd}.csv",
                    cancellationToken);
            }

            await _inAppNotifications.NotifyAdminsAsync(
                "Daily stock digest",
                $"{rows.Count} product line(s) are out of stock or below alert threshold.",
                "StockAlert",
                cancellationToken: cancellationToken);

            _logger.LogInformation("Sent low stock digest for {Count} rows.", rows.Count);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to send low stock digest.");
        }
    }

    private static string BuildStockDigestCsv(IReadOnlyList<StockDigestRow> rows)
    {
        var sb = new StringBuilder();
        sb.AppendLine("ProductId,Title,Sku,Stock,AlertThreshold,Status");
        foreach (var row in rows)
        {
            sb.Append(row.ProductId).Append(',')
                .Append(Csv(row.Title)).Append(',')
                .Append(Csv(row.Sku ?? string.Empty)).Append(',')
                .Append(row.Stock).Append(',')
                .Append(row.AlertThreshold).Append(',')
                .Append(row.IsOutOfStock ? "OutOfStock" : "LowStock")
                .AppendLine();
        }

        return sb.ToString();
    }

    private static string Csv(string value)
    {
        if (value.Contains(',') || value.Contains('"'))
            return $"\"{value.Replace("\"", "\"\"")}\"";
        return value;
    }

    private sealed record StockDigestRow(
        int ProductId,
        string Title,
        string? Sku,
        int Stock,
        int AlertThreshold,
        bool IsOutOfStock);

    private async Task SendStockAlertAsync(
        string productLabel,
        int productId,
        int stockQuantity,
        int minimumAlert,
        bool isOutOfStock,
        CancellationToken cancellationToken)
    {
        var recipients = await ResolveAdminRecipientsAsync(cancellationToken);
        if (recipients.Count == 0)
            return;

        var urgency = isOutOfStock ? "URGENT" : "WARNING";
        var title = isOutOfStock ? "Product out of stock" : "Low stock alert";
        var message = isOutOfStock
            ? $"'{productLabel}' is out of stock."
            : $"'{productLabel}' is low: {stockQuantity} unit(s) left (alert at {minimumAlert}).";

        var adminUrl = $"{_appUrls.FrontendBaseUrl.TrimEnd('/')}/admin/products/{productId}";
        var encodedLabel = WebUtility.HtmlEncode(productLabel);
        var body = $"""
            <p><strong>{title}</strong></p>
            <p>{WebUtility.HtmlEncode(message)}</p>
            <p>Current stock: <strong>{stockQuantity}</strong> · Alert threshold: <strong>{minimumAlert}</strong></p>
            <p><a href="{adminUrl}">View product in admin</a></p>
            """;

        var subject = $"[{urgency}] Stock alert: {productLabel}";
        await SendToRecipientsAsync(recipients, subject, body, cancellationToken);

        await _inAppNotifications.NotifyAdminsAsync(
            title,
            message,
            "StockAlert",
            relatedId: productId,
            cancellationToken: cancellationToken);
    }

    private async Task<IReadOnlyList<string>> ResolveAdminRecipientsAsync(CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(_stockAlerts.AdminEmail))
            return [_stockAlerts.AdminEmail.Trim()];

        return await (
            from user in _db.Users.AsNoTracking()
            join userRole in _db.UserRoles on user.Id equals userRole.UserId
            join role in _db.Roles.AsNoTracking() on userRole.RoleId equals role.Id
            where role.Name == Roles.Admin && user.Email != null && user.Email != ""
            select user.Email!
        ).Distinct().ToListAsync(cancellationToken);
    }

    private async Task SendToRecipientsAsync(
        IReadOnlyList<string> recipients,
        string subject,
        string htmlBody,
        CancellationToken cancellationToken)
    {
        foreach (var recipient in recipients)
        {
            await _email.SendAsync(recipient, subject, htmlBody, cancellationToken);
        }
    }
}
