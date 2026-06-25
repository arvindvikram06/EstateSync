using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using EstateSync.Api.Data;
using EstateSync.Api.Repositories.Interfaces;
using EstateSync.Api.Models;

namespace EstateSync.Api.Repositories;

public class LeaseRepository : BaseRepository<Lease>, ILeaseRepository
{
    public LeaseRepository(AppDbContext context) : base(context)
    {
    }

    public async Task<Lease?> GetActiveLeaseByTenantIdAsync(Guid tenantId)
    {
        return await _dbSet
            .Include(l => l.Property)
            .FirstOrDefaultAsync(l => l.TenantId == tenantId && l.Status == LeaseStatus.Active);
    }

    public async Task<IEnumerable<Lease>> GetActiveLeasesEndingBeforeAsync(DateTime targetDate)
    {
        return await _dbSet
            .Include(l => l.Property)
            .Where(l => l.Status == LeaseStatus.Active && l.EndDate <= targetDate && l.Property != null && l.Property.Status == PropertyStatus.Leased)
            .ToListAsync();
    }

    public async Task<Lease?> GetWithDetailsByIdAsync(Guid id)
    {
        return await _dbSet
            .Include(l => l.Property)
                .ThenInclude(p => p!.Owner)
            .Include(l => l.Tenant)
            .Include(l => l.LeaseDocuments)
            .FirstOrDefaultAsync(l => l.LeaseId == id);
    }

    public async Task<Lease?> GetActiveLeaseByPropertyIdAsync(Guid propertyId)
    {
        return await _dbSet
            .AsNoTracking()
            .FirstOrDefaultAsync(l => l.PropertyId == propertyId && l.Status == LeaseStatus.Active);
    }
    public async Task<Lease?> GetByApplicationIdAsync(Guid applicationId)
    {
        return await _dbSet
            .AsNoTracking()
            .OrderByDescending(l => l.CreatedAt)
            .FirstOrDefaultAsync(l => l.ApplicationId == applicationId);
    }

    public async Task<(IEnumerable<Lease> Items, int Total)> GetByTenantIdPagedAsync(Guid tenantId, int page, int limit)
    {
        var query = _dbSet
            .AsNoTracking()
            .Include(l => l.Property)
            .Include(l => l.Tenant)
            .Where(l => l.TenantId == tenantId);

        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(l => l.CreatedAt)
            .Skip((page - 1) * limit)
            .Take(limit)
            .ToListAsync();

        return (items, total);
    }

    public async Task<(IEnumerable<Lease> Items, int Total)> GetByOwnerIdPagedAsync(Guid ownerId, int page, int limit)
    {
        var query = _dbSet
            .AsNoTracking()
            .Include(l => l.Property)
            .Include(l => l.Tenant)
            .Where(l => l.Property != null && l.Property.OwnerId == ownerId);

        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(l => l.CreatedAt)
            .Skip((page - 1) * limit)
            .Take(limit)
            .ToListAsync();

        return (items, total);
    }

    public async Task<IEnumerable<Lease>> GetActiveLeasesWithBillsAsync()
    {
        return await _dbSet
            .Include(l => l.LeaseBills)
            .Where(l => l.Status == LeaseStatus.Active)
            .ToListAsync();
    }

    public async Task<IEnumerable<Lease>> GetActiveLeasesPastEndDateAsync(DateTime now)
    {
        return await _dbSet
            .Include(l => l.Property)
            .Where(l => l.Status == LeaseStatus.Active && l.EndDate < now)
            .ToListAsync();
    }
}