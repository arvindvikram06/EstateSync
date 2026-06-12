using System.Collections.Generic;
using System.Threading.Tasks;
using EstateSync.Api.Models;

namespace EstateSync.Api.Repositories.Interfaces;

public interface IAuditLogRepository : IRepository<AuditLog>
{
    Task<(IEnumerable<AuditLog> Items, int Total)> GetPagedAsync(
        string? entityType,
        string? entityId,
        string? action,
        int page,
        int limit);
}
