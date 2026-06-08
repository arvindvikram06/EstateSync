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

    public async Task<IEnumerable<RentalApplication>> GetByPropertyIdAsync(Guid propertyId)
    {
        return await _dbSet
            .AsNoTracking()
            .Where(ra => ra.PropertyId == propertyId)
            .ToListAsync();
    }

    public async Task<IEnumerable<RentalApplication>> GetByTenantIdAsync(Guid tenantId)
    {
        return await _dbSet
            .AsNoTracking()
            .Where(ra => ra.TenantId == tenantId)
            .ToListAsync();
    }

    public async Task<bool> HasTenantAppliedAsync(Guid propertyId, Guid tenantId)
    {
        return await _dbSet
            .AnyAsync(ra => ra.PropertyId == propertyId && ra.TenantId == tenantId);
    }

    public async Task<RentalApplication?> GetWithDetailsByIdAsync(Guid applicationId)
    {
        return await _dbSet
            .Include(ra => ra.Property)
            .Include(ra => ra.Tenant)
            .FirstOrDefaultAsync(ra => ra.ApplicationId == applicationId);
    }

    public async Task<IEnumerable<RentalApplication>> GetByOwnerIdAsync(Guid ownerId)
    {
        return await _dbSet
            .AsNoTracking()
            .Include(ra => ra.Property)
            .Include(ra => ra.Tenant)
            .Where(ra => ra.Property != null && ra.Property.OwnerId == ownerId)
            .ToListAsync();
    }

    public async Task<IEnumerable<RentalApplication>> GetByPropertyIdWithDetailsAsync(Guid propertyId)
    {
        return await _dbSet
            .AsNoTracking()
            .Include(ra => ra.Property)
            .Include(ra => ra.Tenant)
            .Where(ra => ra.PropertyId == propertyId)
            .ToListAsync();
    }
}