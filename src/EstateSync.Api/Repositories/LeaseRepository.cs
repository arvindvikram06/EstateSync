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

    public async Task<Lease?> GetWithDetailsByIdAsync(Guid id)
    {
        return await _dbSet
            .AsNoTracking()
            .Include(l => l.Property)
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

    // Reserved for future modules — not yet called by any service
    // public async Task<IEnumerable<Lease>> GetByPropertyIdAsync(Guid propertyId)
    // {
    //     return await _dbSet
    //         .AsNoTracking()
    //         .Where(l => l.PropertyId == propertyId)
    //         .ToListAsync();
    // }

    // public async Task<IEnumerable<Lease>> GetByTenantIdAsync(Guid tenantId)
    // {
    //     return await _dbSet
    //         .AsNoTracking()
    //         .Where(l => l.TenantId == tenantId)
    //         .ToListAsync();
    // }

    // public async Task<IEnumerable<Lease>> GetByOwnerIdAsync(Guid ownerId)
    // {
    //     return await _dbSet
    //         .AsNoTracking()
    //         .Include(l => l.Property)
    //         .Where(l => l.Property != null && l.Property.OwnerId == ownerId)
    //         .ToListAsync();
    // }

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
}