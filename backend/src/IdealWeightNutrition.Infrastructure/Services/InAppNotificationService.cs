using IdealWeightNutrition.Application.Abstractions;
using IdealWeightNutrition.Contracts.Notifications;
using IdealWeightNutrition.Domain.Constants;
using IdealWeightNutrition.Domain.Engagement;
using IdealWeightNutrition.Infrastructure.Options;
using IdealWeightNutrition.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace IdealWeightNutrition.Infrastructure.Services;

internal sealed class InAppNotificationService : IInAppNotificationService
{
    private readonly AppDbContext _db;
    private readonly UserManager<Domain.Identity.ApplicationUser> _userManager;
    private readonly IDateTimeProvider _clock;
    private readonly AppUrlOptions _appUrls;
    private readonly INotificationRealtimePublisher _realtime;

    public InAppNotificationService(
        AppDbContext db,
        UserManager<Domain.Identity.ApplicationUser> userManager,
        IDateTimeProvider clock,
        IOptions<AppUrlOptions> appUrls,
        INotificationRealtimePublisher realtime)
    {
        _db = db;
        _userManager = userManager;
        _clock = clock;
        _appUrls = appUrls.Value;
        _realtime = realtime;
    }

    public async Task NotifyAdminsAsync(
        string title,
        string message,
        string type,
        int? orderId = null,
        int? relatedId = null,
        CancellationToken cancellationToken = default)
    {
        var adminIds = await (
            from user in _db.Users.AsNoTracking()
            join userRole in _db.UserRoles on user.Id equals userRole.UserId
            join role in _db.Roles.AsNoTracking() on userRole.RoleId equals role.Id
            where role.Name == Roles.Admin
            select user.Id
        ).Distinct().ToListAsync(cancellationToken);

        foreach (var adminId in adminIds)
        {
            await NotifyUserAsync(adminId, title, message, type, orderId, relatedId, cancellationToken);
        }
    }

    public async Task NotifyUserAsync(
        string userId,
        string title,
        string message,
        string type,
        int? orderId = null,
        int? relatedId = null,
        CancellationToken cancellationToken = default)
    {
        var (icon, link) = ResolvePresentation(type, orderId, relatedId);
        var entity = new InAppNotification
        {
            UserId = userId,
            Title = title,
            Message = message,
            Type = type,
            OrderId = orderId,
            RelatedId = relatedId,
            IsRead = false,
            CreatedAt = _clock.Now,
            Icon = icon,
            Link = link
        };
        _db.InAppNotifications.Add(entity);
        await _db.SaveChangesAsync(cancellationToken);

        var dto = MapDto(entity);
        await _realtime.PublishToUserAsync(userId, dto, cancellationToken);
    }

    public async Task<IReadOnlyList<NotificationDto>> GetUnreadAsync(
        string userId,
        CancellationToken cancellationToken = default) =>
        await _db.InAppNotifications.AsNoTracking()
            .Where(n => n.UserId == userId && !n.IsRead)
            .OrderByDescending(n => n.CreatedAt)
            .Select(n => new NotificationDto
            {
                Id = n.Id,
                Title = n.Title,
                Message = n.Message,
                Type = n.Type,
                Icon = n.Icon,
                Link = n.Link,
                IsRead = n.IsRead,
                CreatedAt = n.CreatedAt,
                OrderId = n.OrderId,
                RelatedId = n.RelatedId
            })
            .ToListAsync(cancellationToken);

    public async Task<int> GetUnreadCountAsync(string userId, CancellationToken cancellationToken = default) =>
        await _db.InAppNotifications.AsNoTracking()
            .CountAsync(n => n.UserId == userId && !n.IsRead, cancellationToken);

    public async Task MarkAsReadAsync(int notificationId, string userId, CancellationToken cancellationToken = default)
    {
        var notification = await _db.InAppNotifications
            .FirstOrDefaultAsync(n => n.Id == notificationId && n.UserId == userId, cancellationToken)
            ?? throw new InvalidOperationException("Notification not found.");

        if (!notification.IsRead)
        {
            notification.IsRead = true;
            await _db.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task MarkAllAsReadAsync(string userId, CancellationToken cancellationToken = default)
    {
        var unread = await _db.InAppNotifications
            .Where(n => n.UserId == userId && !n.IsRead)
            .ToListAsync(cancellationToken);

        if (unread.Count == 0)
            return;

        foreach (var notification in unread)
            notification.IsRead = true;

        await _db.SaveChangesAsync(cancellationToken);
    }

    private (string Icon, string Link) ResolvePresentation(string type, int? orderId, int? relatedId)
    {
        var baseUrl = _appUrls.FrontendBaseUrl.TrimEnd('/');
        return type switch
        {
            "ReturnRequest" when relatedId is > 0 => ("bi-arrow-return-left", $"{baseUrl}/admin/returns/{relatedId}"),
            "Order" when orderId is > 0 => ("bi-cart-check", $"{baseUrl}/admin/orders/{orderId}"),
            "StockAlert" when relatedId is > 0 => ("bi-exclamation-triangle", $"{baseUrl}/admin/products/{relatedId}"),
            "ServiceSubscription" when relatedId is > 0 => ("bi-briefcase", $"{baseUrl}/admin/service-purchases/{relatedId}"),
            _ when orderId is > 0 => ("bi-bell", $"{baseUrl}/admin/orders/{orderId}"),
            _ => ("bi-bell", $"{baseUrl}/admin")
        };
    }

    private static NotificationDto MapDto(InAppNotification entity) => new()
    {
        Id = entity.Id,
        Title = entity.Title,
        Message = entity.Message,
        Type = entity.Type,
        Icon = entity.Icon,
        Link = entity.Link,
        IsRead = entity.IsRead,
        CreatedAt = entity.CreatedAt,
        OrderId = entity.OrderId,
        RelatedId = entity.RelatedId
    };
}
