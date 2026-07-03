using IdealWeightNutrition.Api.Hubs;
using IdealWeightNutrition.Application.Abstractions;
using IdealWeightNutrition.Contracts.Notifications;
using Microsoft.AspNetCore.SignalR;

namespace IdealWeightNutrition.Api.Services;

internal sealed class NotificationRealtimePublisher : INotificationRealtimePublisher
{
    private readonly IHubContext<NotificationHub> _hub;

    public NotificationRealtimePublisher(IHubContext<NotificationHub> hub) => _hub = hub;

    public Task PublishToUserAsync(
        string userId,
        NotificationDto notification,
        CancellationToken cancellationToken = default) =>
        _hub.Clients.User(userId).SendAsync("ReceiveNotification", notification, cancellationToken);
}
