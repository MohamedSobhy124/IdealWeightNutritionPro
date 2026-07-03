using IdealWeightNutrition.Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace IdealWeightNutrition.Api.Hubs;

[Authorize]
public sealed class NotificationHub : Hub
{
    public const string AdminGroup = "Admins";

    public override async Task OnConnectedAsync()
    {
        if (Context.User?.IsInRole(Roles.Admin) == true)
            await Groups.AddToGroupAsync(Context.ConnectionId, AdminGroup);

        await base.OnConnectedAsync();
    }

    public Task JoinAdminGroup() =>
        Groups.AddToGroupAsync(Context.ConnectionId, AdminGroup);

    public Task LeaveAdminGroup() =>
        Groups.RemoveFromGroupAsync(Context.ConnectionId, AdminGroup);
}
