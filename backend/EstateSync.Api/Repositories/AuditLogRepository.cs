using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using EstateSync.Api.Data;
using EstateSync.Api.Models;
using EstateSync.Api.Repositories.Interfaces;

namespace EstateSync.Api.Repositories;

public class AuditLogRepository : BaseRepository<AuditLog>, IAuditLogRepository
{
    public AuditLogRepository(AppDbContext context) : base(context)
    {
    }

    public async Task<(IEnumerable<AuditLog> Items, int Total)> GetPagedAsync(
        string? entityType,
        string? entityId,
        string? action,
        int page,
        int limit)
    {
        // Internal token management entities are noise for the admin log viewer
        var internalEntityTypes = new[] { "RefreshToken" };

        var query = _dbSet.AsNoTracking()
            .Where(x => !internalEntityTypes.Contains(x.EntityType));

        if (!string.IsNullOrWhiteSpace(entityType))
        {
            query = query.Where(x => x.EntityType == entityType);
        }

        if (!string.IsNullOrWhiteSpace(entityId))
        {
            query = query.Where(x => x.EntityId == entityId);
        }

        if (!string.IsNullOrWhiteSpace(action))
        {
            query = query.Where(x => x.Action == action);
        }

        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(x => x.CreatedAt)
            .Skip((page - 1) * limit)
            .Take(limit)
            .ToListAsync();

        return (items, total);
    }
}
