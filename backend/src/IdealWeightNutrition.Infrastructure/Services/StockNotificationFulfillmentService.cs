using System.Net;
using IdealWeightNutrition.Application.Abstractions;
using IdealWeightNutrition.Domain.Catalogue;
using IdealWeightNutrition.Domain.Engagement;
using IdealWeightNutrition.Infrastructure.Options;
using IdealWeightNutrition.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace IdealWeightNutrition.Infrastructure.Services;

internal sealed class StockNotificationFulfillmentService : IStockNotificationFulfillmentService
{
    private readonly AppDbContext _db;
    private readonly IEmailService _email;
    private readonly IDateTimeProvider _clock;
    private readonly AppUrlOptions _urls;
    private readonly ILogger<StockNotificationFulfillmentService> _logger;

    public StockNotificationFulfillmentService(
        AppDbContext db,
        IEmailService email,
        IDateTimeProvider clock,
        IOptions<AppUrlOptions> urls,
        ILogger<StockNotificationFulfillmentService> logger)
    {
        _db = db;
        _email = email;
        _clock = clock;
        _urls = urls.Value;
        _logger = logger;
    }

    public Task<int> ProcessPendingAsync(CancellationToken cancellationToken = default) =>
        ProcessBatchAsync(productId: null, productVariantId: null, cancellationToken);

    public Task<int> ProcessProductRestockedAsync(
        int productId,
        int? productVariantId = null,
        CancellationToken cancellationToken = default) =>
        ProcessBatchAsync(productId, productVariantId, cancellationToken);

    private async Task<int> ProcessBatchAsync(
        int? productId,
        int? productVariantId,
        CancellationToken cancellationToken)
    {
        var query = _db.StockNotifications
            .Where(s => !s.IsDeleted && s.IsActive && !s.IsNotified);

        if (productId is > 0)
            query = query.Where(s => s.ProductId == productId);

        if (productVariantId is > 0)
            query = query.Where(s => s.ProductVariantId == productVariantId);

        var pending = await query
            .OrderBy(s => s.CreatedDate)
            .Take(200)
            .ToListAsync(cancellationToken);

        if (pending.Count == 0)
            return 0;

        var productIds = pending.Select(s => s.ProductId).Distinct().ToList();
        var products = await _db.Products.AsNoTracking()
            .Where(p => productIds.Contains(p.Id) && !p.IsDeleted)
            .ToDictionaryAsync(p => p.Id, cancellationToken);

        var variantIds = pending
            .Where(s => s.ProductVariantId is > 0)
            .Select(s => s.ProductVariantId!.Value)
            .Distinct()
            .ToList();

        var variants = variantIds.Count == 0
            ? new Dictionary<int, ProductVariant>()
            : await _db.Set<ProductVariant>().AsNoTracking()
                .Where(v => variantIds.Contains(v.Id) && !v.IsDeleted)
                .ToDictionaryAsync(v => v.Id, cancellationToken);

        var variableProductIds = products.Values
            .Where(p => p.ProductType == ProductType.Variable)
            .Select(p => p.Id)
            .ToList();

        var variableStock = variableProductIds.Count == 0
            ? new Dictionary<int, bool>()
            : await _db.Set<ProductVariant>().AsNoTracking()
                .Where(v => variableProductIds.Contains(v.ProductId) && !v.IsDeleted)
                .GroupBy(v => v.ProductId)
                .Select(g => new { ProductId = g.Key, InStock = g.Any(v => v.StockQuantity > 0) })
                .ToDictionaryAsync(x => x.ProductId, x => x.InStock, cancellationToken);

        var sent = 0;
        foreach (var notification in pending)
        {
            if (!products.TryGetValue(notification.ProductId, out var product))
                continue;

            if (!IsInStock(notification, product, variants, variableStock))
                continue;

            try
            {
                await SendCustomerEmailAsync(notification, product, variants, cancellationToken);
                notification.IsNotified = true;
                notification.NotifiedDate = _clock.Now;
                notification.ModifiedDate = _clock.Now;
                sent++;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    ex,
                    "Failed to send back-in-stock email for stock notification #{NotificationId}",
                    notification.Id);
            }
        }

        if (sent > 0)
            await _db.SaveChangesAsync(cancellationToken);

        if (sent > 0)
            _logger.LogInformation("Sent {Count} back-in-stock customer notification(s).", sent);

        return sent;
    }

    private static bool IsInStock(
        StockNotification notification,
        Product product,
        IReadOnlyDictionary<int, ProductVariant> variants,
        IReadOnlyDictionary<int, bool> variableStock)
    {
        if (notification.ProductVariantId is > 0)
        {
            return variants.TryGetValue(notification.ProductVariantId.Value, out var variant)
                && variant.StockQuantity > 0;
        }

        if (product.ProductType == ProductType.Variable)
            return variableStock.TryGetValue(product.Id, out var inStock) && inStock;

        return product.StockQuantity > 0;
    }

    private async Task SendCustomerEmailAsync(
        StockNotification notification,
        Product product,
        IReadOnlyDictionary<int, ProductVariant> variants,
        CancellationToken cancellationToken)
    {
        var slug = product.GetSlug();
        var productUrl = $"{_urls.FrontendBaseUrl.TrimEnd('/')}/product/{slug}";
        var encodedTitle = WebUtility.HtmlEncode(product.Title);

        string? variantLabel = null;
        if (notification.ProductVariantId is > 0
            && variants.TryGetValue(notification.ProductVariantId.Value, out var variant)
            && !string.IsNullOrWhiteSpace(variant.Sku))
        {
            variantLabel = variant.Sku;
        }

        var variantLine = variantLabel is null
            ? string.Empty
            : $"<p>Variant: <strong>{WebUtility.HtmlEncode(variantLabel)}</strong></p>";

        var body = $"""
            <p>Good news!</p>
            <p><strong>{encodedTitle}</strong> is back in stock and ready to order.</p>
            {variantLine}
            <p><a href="{productUrl}">Shop now</a></p>
            <p style="color:#6b7280;font-size:0.9rem;">You received this email because you asked to be notified when this product is available again.</p>
            """;

        var subject = $"Back in stock: {product.Title}";
        await _email.SendAsync(notification.Email, subject, body, cancellationToken);
    }
}
