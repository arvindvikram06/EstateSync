using EstateSync.Api.Data;
using EstateSync.Api.Models;
using EstateSync.Api.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EstateSync.Api.Repositories;

public class SiteVisitRepository : BaseRepository<SiteVisit>, ISiteVisitRepository
{
    public SiteVisitRepository(AppDbContext context) : base(context) { }

    private IQueryable<SiteVisit> Details() => _dbSet
        .Include(v => v.Property)
        .Include(v => v.Tenant)
        .Include(v => v.PropertyVisitSlot)
        .Include(v => v.PreviousPropertyVisitSlot);

    public async Task<IEnumerable<SiteVisit>> GetByTenantIdAsync(Guid tenantId) =>
        await Details().AsNoTracking()
            .Where(v => v.TenantId == tenantId)
            .OrderBy(v => v.VisitDate).ThenBy(v => v.VisitTime)
            .ToListAsync();

    public async Task<IEnumerable<SiteVisit>> GetByOwnerIdAsync(Guid ownerId) =>
        await Details().AsNoTracking()
            .Where(v => v.Property != null && v.Property.OwnerId == ownerId)
            .OrderBy(v => v.VisitDate).ThenBy(v => v.VisitTime)
            .ToListAsync();

    public Task<SiteVisit?> GetWithDetailsByIdAsync(Guid id) =>
        Details().FirstOrDefaultAsync(v => v.SiteVisitId == id);

    public Task<bool> HasActiveRequestAsync(Guid tenantId, Guid propertyId) =>
        _dbSet.AnyAsync(v =>
            v.TenantId == tenantId &&
            v.PropertyId == propertyId &&
            (v.Status == SiteVisitStatus.PendingOwnerApproval ||
             v.Status == SiteVisitStatus.PendingTenantApproval ||
             v.Status == SiteVisitStatus.Approved));
}
