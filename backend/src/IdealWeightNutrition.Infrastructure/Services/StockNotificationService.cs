using System.Text.RegularExpressions;
using IdealWeightNutrition.Application.Abstractions;
using IdealWeightNutrition.Contracts.Stock;
using IdealWeightNutrition.Domain.Engagement;
using IdealWeightNutrition.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace IdealWeightNutrition.Infrastructure.Services;

internal sealed partial class StockNotificationService : IStockNotificationService
{
    private readonly AppDbContext _db;
    private readonly IDateTimeProvider _clock;
    private readonly IAdminNotificationService _notifications;

    public StockNotificationService(
        AppDbContext db,
        IDateTimeProvider clock,
        IAdminNotificationService notifications)
    {
        _db = db;
        _clock = clock;
        _notifications = notifications;
    }

    public async Task<StockNotificationSubscribeResponse> SubscribeAsync(
        int productId,
        string? email,
        string? phoneNumber,
        int? productVariantId,
        string? userId,
        CancellationToken cancellationToken = default)
    {
        var product = await _db.Products.AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == productId && !p.IsDeleted, cancellationToken)
            ?? throw new InvalidOperationException("Product not found.");

        if (product.StockQuantity > 0)
            throw new InvalidOperationException("This product is currently in stock.");

        var normalizedEmail = email?.Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(normalizedEmail) || !IsValidEmail(normalizedEmail))
            throw new InvalidOperationException("A valid email address is required.");

        var existing = await _db.StockNotifications.FirstOrDefaultAsync(
            s => s.ProductId == productId
                 && s.Email == normalizedEmail
                 && s.ProductVariantId == productVariantId
                 && !s.IsDeleted,
            cancellationToken);

        if (existing is not null)
        {
            if (existing.IsActive)
                throw new InvalidOperationException("You are already subscribed for this product.");

            existing.IsActive = true;
            existing.PhoneNumber = phoneNumber?.Trim();
            existing.ModifiedDate = _clock.Now;
            await _db.SaveChangesAsync(cancellationToken);
            await _notifications.NotifyStockNotificationRequestAsync(
                productId, normalizedEmail, phoneNumber, productVariantId, cancellationToken);

            return new StockNotificationSubscribeResponse
            {
                Message = "We will notify you when this product is back in stock."
            };
        }

        var notification = new StockNotification
        {
            ProductId = productId,
            ProductVariantId = productVariantId,
            Email = normalizedEmail,
            PhoneNumber = phoneNumber?.Trim(),
            ApplicationUserId = userId,
            IsActive = true,
            IsNotified = false,
            CreatedDate = _clock.Now,
            IsDeleted = false
        };

        _db.StockNotifications.Add(notification);
        await _db.SaveChangesAsync(cancellationToken);

        await _notifications.NotifyStockNotificationRequestAsync(
            productId, normalizedEmail, phoneNumber, productVariantId, cancellationToken);

        return new StockNotificationSubscribeResponse
        {
            Message = "We will notify you when this product is back in stock."
        };
    }

    private static bool IsValidEmail(string email) => EmailRegex().IsMatch(email);

    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.IgnoreCase)]
    private static partial Regex EmailRegex();
}
