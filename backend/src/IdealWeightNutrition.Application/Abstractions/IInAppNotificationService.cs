using IdealWeightNutrition.Contracts.Notifications;

namespace IdealWeightNutrition.Application.Abstractions;

public interface IInAppNotificationService
{
    Task NotifyAdminsAsync(
        string title,
        string message,
        string type,
        int? orderId = null,
        int? relatedId = null,
        CancellationToken cancellationToken = default);

    Task NotifyUserAsync(
        string userId,
        string title,
        string message,
        string type,
        int? orderId = null,
        int? relatedId = null,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<NotificationDto>> GetUnreadAsync(
        string userId,
        CancellationToken cancellationToken = default);

    Task<int> GetUnreadCountAsync(string userId, CancellationToken cancellationToken = default);

    Task MarkAsReadAsync(int notificationId, string userId, CancellationToken cancellationToken = default);

    Task MarkAllAsReadAsync(string userId, CancellationToken cancellationToken = default);
}
