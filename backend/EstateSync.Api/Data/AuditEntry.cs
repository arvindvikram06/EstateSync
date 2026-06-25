using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using EstateSync.Api.Models;

namespace EstateSync.Api.Data;

public class AuditEntry
{
    public AuditEntry(EntityEntry entry)
    {
        Entry = entry;
    }

    public EntityEntry Entry { get; }
    public string? EntityId { get; set; }
    public string? EntityName { get; set; }
    public string? EntityType { get; set; }
    public string? Action { get; set; }
    public Dictionary<string, object?> OldValues { get; } = new();
    public Dictionary<string, object?> NewValues { get; } = new();
    public List<PropertyEntry> TemporaryProperties { get; } = new();

    public bool HasTemporaryProperties => TemporaryProperties.Count > 0;

    public AuditLog ToAuditLog(string? ipAddress)
    {
        return new AuditLog
        {
            EntityId = EntityId,
            EntityName = EntityName,
            EntityType = EntityType,
            Action = Action,
            OldValues = OldValues.Count == 0 ? null : JsonSerializer.Serialize(OldValues),
            NewValues = NewValues.Count == 0 ? null : JsonSerializer.Serialize(NewValues),
            IPAddress = ipAddress,
            CreatedAt = DateTime.UtcNow
        };
    }
}
