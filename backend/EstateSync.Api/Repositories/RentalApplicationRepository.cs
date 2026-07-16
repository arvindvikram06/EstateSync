using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using EstateSync.Api.Data;
using EstateSync.Api.Repositories.Interfaces;
using EstateSync.Api.Models;

namespace EstateSync.Api.Repositories;

public class RentalApplicationRepository : BaseRepository<RentalApplication>, IRentalApplicationRepository
{
    public RentalApplicationRepository(AppDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<RentalApplication>> GetByPropertyIdForUpdateAsync(Guid propertyId)
    {
        return await _dbSet
            .Where(ra => ra.PropertyId == propertyId)
            .ToListAsync();
    }


    public async Task<bool> HasTenantAppliedAsync(Guid propertyId, Guid tenantId)
    {
        return await _dbSet
            .AnyAsync(ra => ra.PropertyId == propertyId
                         && ra.TenantId == tenantId
                         && ra.Status != ApplicationStatus.Rejected
                         && ra.Status != ApplicationStatus.Withdrawn);
    }

    public async Task<RentalApplication?> GetRejectedOrWithdrawnAsync(Guid propertyId, Guid tenantId)
    {
        return await _dbSet
            .FirstOrDefaultAsync(ra => ra.PropertyId == propertyId
                                    && ra.TenantId == tenantId
                                    && (ra.Status == ApplicationStatus.Rejected
                                     || ra.Status == ApplicationStatus.Withdrawn));
    }

    public async Task<RentalApplication?> GetWithDetailsByIdAsync(Guid applicationId)
    {
        return await _dbSet
            .Include(ra => ra.Property)
                .ThenInclude(p => p!.Owner)
            .Include(ra => ra.Tenant)
            .Include(ra => ra.Leases)
            .FirstOrDefaultAsync(ra => ra.ApplicationId == applicationId);
    }

    public async Task<(IEnumerable<RentalApplication> Items, int Total)> GetByOwnerIdPagedAsync(Guid ownerId, int page, int limit)
    {
        var query = _dbSet
            .AsNoTracking()
            .Include(ra => ra.Property)
            .Include(ra => ra.Tenant)
            .Include(ra => ra.Leases)
            .Where(ra => ra.Property != null && ra.Property.OwnerId == ownerId);

        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(ra => ra.CreatedAt)
            .Skip((page - 1) * limit)
            .Take(limit)
            .ToListAsync();

        return (items, total);
    }

    public async Task<(IEnumerable<RentalApplication> Items, int Total)> GetByPropertyIdWithDetailsPagedAsync(Guid propertyId, int page, int limit)
    {
        var query = _dbSet
            .AsNoTracking()
            .Include(ra => ra.Property)
            .Include(ra => ra.Tenant)
            .Include(ra => ra.Leases)
            .Where(ra => ra.PropertyId == propertyId);

        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(ra => ra.CreatedAt)
            .Skip((page - 1) * limit)
            .Take(limit)
            .ToListAsync();

        return (items, total);
    }

    public async Task<(IEnumerable<RentalApplication> Items, int Total)> GetByTenantIdWithDetailsPagedAsync(Guid tenantId, int page, int limit)
    {
        var query = _dbSet
            .AsNoTracking()
            .Include(ra => ra.Property)
            .Include(ra => ra.Tenant)
            .Include(ra => ra.Leases)
            .Where(ra => ra.TenantId == tenantId);

        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(ra => ra.CreatedAt)
            .Skip((page - 1) * limit)
            .Take(limit)
            .ToListAsync();

        return (items, total);
    }
}