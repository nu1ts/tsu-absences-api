using Microsoft.AspNetCore.SignalR;
using SignalRSwaggerGen.Attributes;

namespace tsu_absences_api.Hubs;

[SignalRHub("/notification")]
public class NotificationHub : Hub
{
    [SignalRMethod("join")]
    public async Task JoinGroup(string groupName)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
    }

    [SignalRMethod("leave")]
    public async Task LeaveGroup(string groupName)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
    }
}