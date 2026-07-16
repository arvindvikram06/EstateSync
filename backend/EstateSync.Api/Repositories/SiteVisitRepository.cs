using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using EstateSync.Api.Data;
using EstateSync.Api.Models;
using EstateSync.Api.Repositories.Interfaces;

namespace EstateSync.Api.Repositories;

public class SiteVisitRepository : BaseRepository<SiteVisit>, ISiteVisitRepository
{
    public SiteVisitRepository(AppDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<SiteVisit>> GetByTenantIdAsync(Guid tenantId)
    {
        return await _dbSet
            .AsNoTracking()
            .Include(v => v.Property)
            .Where(v => v.TenantId == tenantId)
            .OrderBy(v => v.VisitDate)
            .ThenBy(v => v.VisitTime)
            .ToListAsync();
    }

    public async Task<IEnumerable<SiteVisit>> GetByOwnerIdAsync(Guid ownerId)
    {
        return await _dbSet
            .AsNoTracking()
            .Include(v => v.Property)
            .Include(v => v.Tenant)
            .Where(v => v.Property != null && v.Property.OwnerId == ownerId)
            .OrderBy(v => v.VisitDate)
            .ThenBy(v => v.VisitTime)
            .ToListAsync();
    }

    public async Task<SiteVisit?> GetWithDetailsByIdAsync(Guid id)
    {
        return await _dbSet
            .Include(v => v.Property)
            .Include(v => v.Tenant)
            .FirstOrDefaultAsync(v => v.SiteVisitId == id);
    }

    public async Task<bool> HasActiveRequestAsync(Guid tenantId, Guid propertyId)
    {
        return await _dbSet.AnyAsync(v =>
            v.TenantId == tenantId &&
            v.PropertyId == propertyId &&
            (v.Status == SiteVisitStatus.Pending || v.Status == SiteVisitStatus.Approved));
    }
}
