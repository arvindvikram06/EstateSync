using EstateSync.Api.DTOs.AuditLogs;
using EstateSync.Api.Models;

namespace EstateSync.Api.Mappers;

public class AuditLogMapper
{
    public AuditLogResponse ToResponse(AuditLog log)
    {
        return new AuditLogResponse(
            log.AuditLogId,
            log.EntityId,
            log.EntityName,
            log.EntityType,
            log.Action,
            log.OldValues,
            log.NewValues,
            log.IPAddress,
            log.CreatedAt
        );
    }
}
