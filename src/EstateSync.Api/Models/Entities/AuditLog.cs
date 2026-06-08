using System;

namespace EstateSync.Api.Models;

public class AuditLog
{
    public long AuditLogId { get; set; }
    public string? EntityId { get; set; }
    public string? EntityName { get; set; }
    public string? EntityType { get; set; }
    public string? Action { get; set; }
    public string? OldValues { get; set; }
    public string? NewValues { get; set; }
    public string? IPAddress { get; set; }
    public DateTime CreatedAt { get; set; }
}
