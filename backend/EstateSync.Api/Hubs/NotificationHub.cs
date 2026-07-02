using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace EstateSync.Api.Hubs;

[Authorize]
public class NotificationHub : Hub
{
}
