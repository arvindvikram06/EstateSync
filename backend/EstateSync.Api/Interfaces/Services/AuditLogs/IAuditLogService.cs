using System.Threading;
using System.Threading.Tasks;
using EstateSync.Api.Common;
using EstateSync.Api.DTOs.AuditLogs;

namespace EstateSync.Api.Interfaces.Services.AuditLogs;

public interface IAuditLogService
{
    Task<PagedResponse<AuditLogResponse>> GetPagedLogsAsync(
        string? entityType,
        string? entityId,
        string? action,
        int page,
        int limit,
        CancellationToken ct = default);
}
