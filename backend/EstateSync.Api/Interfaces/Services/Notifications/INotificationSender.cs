using EstateSync.Api.Models;

namespace EstateSync.Api.Interfaces.Services.Notifications;

public interface INotificationSender
{
    Task SendAsync(Notification notification, CancellationToken ct = default);
}
