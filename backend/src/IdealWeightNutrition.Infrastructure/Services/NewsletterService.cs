using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using IdealWeightNutrition.Application.Abstractions;
using IdealWeightNutrition.Contracts.Newsletter;
using IdealWeightNutrition.Domain.Engagement;
using IdealWeightNutrition.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace IdealWeightNutrition.Infrastructure.Services;

internal sealed partial class NewsletterService : INewsletterService
{
    private readonly AppDbContext _db;
    private readonly IDateTimeProvider _clock;

    public NewsletterService(AppDbContext db, IDateTimeProvider clock)
    {
        _db = db;
        _clock = clock;
    }

    public async Task<NewsletterSubscribeResponse> SubscribeAsync(
        string email,
        string? source,
        CancellationToken cancellationToken = default)
    {
        var normalized = NormalizeEmail(email);
        if (!IsValidEmail(normalized))
            throw new InvalidOperationException("Please enter a valid email address.");

        var existing = await _db.NewsletterSubscriptions
            .FirstOrDefaultAsync(n => n.Email == normalized, cancellationToken);

        if (existing is not null)
        {
            if (existing.IsActive)
                throw new InvalidOperationException("This email is already subscribed.");

            existing.IsActive = true;
            existing.SubscribedDate = _clock.Now;
            existing.UnsubscribedDate = null;
            existing.Source = source ?? existing.Source;
            await _db.SaveChangesAsync(cancellationToken);

            return new NewsletterSubscribeResponse
            {
                Message = "Thank you for subscribing!",
                IsReactivated = true
            };
        }

        _db.NewsletterSubscriptions.Add(new NewsletterSubscription
        {
            Email = normalized,
            SubscribedDate = _clock.Now,
            IsActive = true,
            Source = source ?? "Storefront"
        });
        await _db.SaveChangesAsync(cancellationToken);

        return new NewsletterSubscribeResponse
        {
            Message = "Thank you for subscribing!",
            IsReactivated = false
        };
    }

    public async Task<IReadOnlyList<NewsletterSubscriptionDto>> ListSubscriptionsAsync(
        string? status,
        CancellationToken cancellationToken = default)
    {
        var query = _db.NewsletterSubscriptions.AsNoTracking();

        if (string.Equals(status, "active", StringComparison.OrdinalIgnoreCase))
            query = query.Where(n => n.IsActive);
        else if (string.Equals(status, "inactive", StringComparison.OrdinalIgnoreCase))
            query = query.Where(n => !n.IsActive);

        return await query
            .OrderByDescending(n => n.SubscribedDate)
            .Take(500)
            .Select(n => new NewsletterSubscriptionDto
            {
                Id = n.Id,
                Email = n.Email,
                SubscribedDate = n.SubscribedDate,
                IsActive = n.IsActive,
                UnsubscribedDate = n.UnsubscribedDate,
                Source = n.Source
            })
            .ToListAsync(cancellationToken);
    }

    public async Task ToggleActiveAsync(int id, CancellationToken cancellationToken = default)
    {
        var subscription = await _db.NewsletterSubscriptions.FirstOrDefaultAsync(n => n.Id == id, cancellationToken)
            ?? throw new InvalidOperationException("Subscription not found.");

        subscription.IsActive = !subscription.IsActive;
        if (subscription.IsActive)
        {
            subscription.UnsubscribedDate = null;
            subscription.SubscribedDate = _clock.Now;
        }
        else
        {
            subscription.UnsubscribedDate = _clock.Now;
        }

        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var subscription = await _db.NewsletterSubscriptions.FirstOrDefaultAsync(n => n.Id == id, cancellationToken)
            ?? throw new InvalidOperationException("Subscription not found.");

        _db.NewsletterSubscriptions.Remove(subscription);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<byte[]> ExportActiveCsvAsync(CancellationToken cancellationToken = default)
    {
        var rows = await _db.NewsletterSubscriptions.AsNoTracking()
            .Where(n => n.IsActive)
            .OrderByDescending(n => n.SubscribedDate)
            .Select(n => new { n.Email, n.SubscribedDate, n.Source })
            .ToListAsync(cancellationToken);

        var csv = new StringBuilder();
        csv.AppendLine("Email,Subscribed Date,Source");
        foreach (var row in rows)
        {
            var source = row.Source ?? "N/A";
            csv.Append(CultureInfo.InvariantCulture, $"{row.Email},{row.SubscribedDate:yyyy-MM-dd HH:mm:ss},{source}");
            csv.AppendLine();
        }

        return Encoding.UTF8.GetBytes(csv.ToString());
    }

    public async Task<NewsletterStatusResponse> GetStatusAsync(
        string email,
        CancellationToken cancellationToken = default)
    {
        var normalized = NormalizeEmail(email);
        var subscription = await _db.NewsletterSubscriptions
            .AsNoTracking()
            .FirstOrDefaultAsync(n => n.Email == normalized, cancellationToken);

        return new NewsletterStatusResponse
        {
            IsSubscribed = subscription is not null && subscription.IsActive
        };
    }

    public async Task<NewsletterUnsubscribeResponse> UnsubscribeAsync(
        string email,
        CancellationToken cancellationToken = default)
    {
        var normalized = NormalizeEmail(email);
        var subscription = await _db.NewsletterSubscriptions
            .FirstOrDefaultAsync(n => n.Email == normalized, cancellationToken)
            ?? throw new InvalidOperationException("Subscription not found.");

        if (!subscription.IsActive)
            throw new InvalidOperationException("You are already unsubscribed.");

        subscription.IsActive = false;
        subscription.UnsubscribedDate = _clock.Now;
        await _db.SaveChangesAsync(cancellationToken);

        return new NewsletterUnsubscribeResponse
        {
            Message = "You have been unsubscribed from our newsletter."
        };
    }

    private static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();

    private static bool IsValidEmail(string email) => EmailRegex().IsMatch(email);

    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.IgnoreCase)]
    private static partial Regex EmailRegex();
}
