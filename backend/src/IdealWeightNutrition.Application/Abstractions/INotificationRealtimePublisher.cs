using IdealWeightNutrition.Contracts.Notifications;

namespace IdealWeightNutrition.Application.Abstractions;

public interface INotificationRealtimePublisher
{
    Task PublishToUserAsync(string userId, NotificationDto notification, CancellationToken cancellationToken = default);
}
