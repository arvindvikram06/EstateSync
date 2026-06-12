using System;

namespace EstateSync.Api.DTOs.AuditLogs;

public record AuditLogResponse(
    long AuditLogId,
    string? EntityId,
    string? EntityName,
    string? EntityType,
    string? Action,
    string? OldValues,
    string? NewValues,
    string? IPAddress,
    DateTime CreatedAt
);
