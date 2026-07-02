using EstateSync.Api.DTOs.Notifications;
using EstateSync.Api.Hubs;
using EstateSync.Api.Interfaces.Services.Notifications;
using EstateSync.Api.Models;
using EstateSync.Api.Repositories.Interfaces;
using Microsoft.AspNetCore.SignalR;

namespace EstateSync.Api.Services.Notifications;

public class NotificationSender : INotificationSender
{
    private readonly INotificationRepository _notificationRepo;
    private readonly IHubContext<NotificationHub> _hubContext;

    public NotificationSender(INotificationRepository notificationRepo, IHubContext<NotificationHub> hubContext)
    {
        _notificationRepo = notificationRepo ?? throw new ArgumentNullException(nameof(notificationRepo));
        _hubContext = hubContext ?? throw new ArgumentNullException(nameof(hubContext));
    }

    public async Task SendAsync(Notification notification, CancellationToken ct = default)
    {
        await _notificationRepo.AddAsync(notification);

        var dto = new NotificationResponse
        {
            NotificationId = notification.NotificationId,
            Type = notification.Type,
            Title = notification.Title,
            Message = notification.Message,
            EntityId = notification.EntityId,
            EntityType = notification.EntityType,
            IsRead = notification.IsRead,
            CreatedAt = notification.CreatedAt
        };

        await _hubContext.Clients
            .User(notification.UserId.ToString())
            .SendAsync("ReceiveNotification", dto, ct);
    }
}
