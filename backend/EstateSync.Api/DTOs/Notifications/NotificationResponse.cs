using System;
using EstateSync.Api.Models;

namespace EstateSync.Api.DTOs.Notifications;

public class NotificationResponse
{
    public Guid NotificationId { get; set; }
    public NotificationType Type { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? EntityId { get; set; }
    public string? EntityType { get; set; }
    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; }
}
