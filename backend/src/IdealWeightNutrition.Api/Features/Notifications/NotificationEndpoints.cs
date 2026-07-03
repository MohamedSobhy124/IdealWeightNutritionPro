using FastEndpoints;

using IdealWeightNutrition.Api.Http;

using IdealWeightNutrition.Application.Abstractions;

using IdealWeightNutrition.Contracts.Notifications;



namespace IdealWeightNutrition.Api.Features.Notifications;



public sealed class GetUnreadNotificationsEndpoint : EndpointWithoutRequest<IReadOnlyList<NotificationDto>>

{

    private readonly IInAppNotificationService _notifications;



    public GetUnreadNotificationsEndpoint(IInAppNotificationService notifications) => _notifications = notifications;



    public override void Configure()

    {

        Get("notifications/unread");

    }



    public override async Task HandleAsync(CancellationToken ct)

    {

        var userId = CartHttp.GetUserId(User);

        if (string.IsNullOrEmpty(userId))

        {

            ThrowError("Unauthorized.", StatusCodes.Status401Unauthorized);

            return;

        }



        await Send.OkAsync(await _notifications.GetUnreadAsync(userId, ct), ct);

    }

}



public sealed class GetNotificationCountEndpoint : EndpointWithoutRequest<NotificationCountDto>

{

    private readonly IInAppNotificationService _notifications;



    public GetNotificationCountEndpoint(IInAppNotificationService notifications) => _notifications = notifications;



    public override void Configure()

    {

        Get("notifications/count");

    }



    public override async Task HandleAsync(CancellationToken ct)

    {

        var userId = CartHttp.GetUserId(User);

        if (string.IsNullOrEmpty(userId))

        {

            ThrowError("Unauthorized.", StatusCodes.Status401Unauthorized);

            return;

        }



        var count = await _notifications.GetUnreadCountAsync(userId, ct);

        await Send.OkAsync(new NotificationCountDto { Count = count }, ct);

    }

}



public sealed class MarkNotificationReadEndpoint : EndpointWithoutRequest<NotificationActionResponse>

{

    private readonly IInAppNotificationService _notifications;



    public MarkNotificationReadEndpoint(IInAppNotificationService notifications) => _notifications = notifications;



    public override void Configure()

    {

        Post("notifications/{id}/mark-read");

    }



    public override async Task HandleAsync(CancellationToken ct)

    {

        var userId = CartHttp.GetUserId(User);

        if (string.IsNullOrEmpty(userId))

        {

            ThrowError("Unauthorized.", StatusCodes.Status401Unauthorized);

            return;

        }



        if (!int.TryParse(Route<string>("id"), out var id) || id <= 0)

        {

            ThrowError("Invalid notification id.", StatusCodes.Status400BadRequest);

            return;

        }



        try

        {

            await _notifications.MarkAsReadAsync(id, userId, ct);

            await Send.OkAsync(new NotificationActionResponse { Success = true }, ct);

        }

        catch (InvalidOperationException ex)

        {

            ThrowError(ex.Message, StatusCodes.Status404NotFound);

        }

    }

}



public sealed class MarkAllNotificationsReadEndpoint : EndpointWithoutRequest<NotificationActionResponse>

{

    private readonly IInAppNotificationService _notifications;



    public MarkAllNotificationsReadEndpoint(IInAppNotificationService notifications) => _notifications = notifications;



    public override void Configure()

    {

        Post("notifications/mark-all-read");

    }



    public override async Task HandleAsync(CancellationToken ct)

    {

        var userId = CartHttp.GetUserId(User);

        if (string.IsNullOrEmpty(userId))

        {

            ThrowError("Unauthorized.", StatusCodes.Status401Unauthorized);

            return;

        }



        await _notifications.MarkAllAsReadAsync(userId, ct);

        await Send.OkAsync(new NotificationActionResponse { Success = true }, ct);

    }

}

