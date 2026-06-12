using System;
using EstateSync.Api.Common;

namespace EstateSync.Api.Models;

public class Notification : BaseEntity
{
    public Guid NotificationId { get; set; }
    public Guid UserId { get; set; }
    public User? User { get; set; }
    public NotificationType Type { get; set; }
    public required string Title { get; set; }
    public required string Message { get; set; }
    public string? EntityId { get; set; }
    public string? EntityType { get; set; }
    public bool IsRead { get; set; }
    public DateTime? ReadAt { get; set; }
}
