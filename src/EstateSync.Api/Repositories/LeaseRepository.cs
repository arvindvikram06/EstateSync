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

    public async Task<IEnumerable<Lease>> GetByPropertyIdAsync(Guid propertyId)
    {
        return await _dbSet
            .AsNoTracking()
            .Where(l => l.PropertyId == propertyId)
            .ToListAsync();
    }

    public async Task<Lease?> GetActiveLeaseByPropertyIdAsync(Guid propertyId)
    {
        return await _dbSet
            .AsNoTracking()
            .FirstOrDefaultAsync(l => l.PropertyId == propertyId && l.Status == LeaseStatus.Active);
    }

    public async Task<IEnumerable<Lease>> GetByTenantIdAsync(Guid tenantId)
    {
        return await _dbSet
            .AsNoTracking()
            .Where(l => l.TenantId == tenantId)
            .ToListAsync();
    }

    public async Task<IEnumerable<Lease>> GetByOwnerIdAsync(Guid ownerId)
    {
        return await _dbSet
            .AsNoTracking()
            .Include(l => l.Property)
            .Where(l => l.Property != null && l.Property.OwnerId == ownerId)
            .ToListAsync();
    }
}