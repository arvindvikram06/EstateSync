using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using EstateSync.Api.Data;
using EstateSync.Api.Repositories.Interfaces;
using EstateSync.Api.Models;

namespace EstateSync.Api.Repositories;

public class LeaseBillRepository : BaseRepository<LeaseBill>, ILeaseBillRepository
{
    public LeaseBillRepository(AppDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<LeaseBill>> GetByLeaseIdAsync(Guid leaseId)
    {
        return await _dbSet
            .AsNoTracking()
            .Where(b => b.LeaseId == leaseId)
            .ToListAsync();
    }

    public async Task<IEnumerable<LeaseBill>> GetOverdueBillsAsync(DateTime currentDate)
    {
        return await _dbSet
            .AsNoTracking()
            .Where(b => b.DueDate < currentDate && b.PendingAmount > 0)
            .ToListAsync();
    }

    public async Task<IEnumerable<LeaseBill>> GetOverdueBillsWithLeaseAsync(DateTime currentDate)
    {
        return await _dbSet
            .Include(b => b.Lease)
                .ThenInclude(l => l!.LeaseBills)
            .Include(b => b.Lease)
                .ThenInclude(l => l!.Tenant)
            .Where(b => b.DueDate < currentDate && b.PendingAmount > 0 && b.BillType == LeaseBillType.Rent)
            .ToListAsync();
    }

    public async Task<LeaseBill?> GetByIdWithLeaseAndPropertyAsync(Guid billId)
    {
        return await _dbSet
            .Include(b => b.Lease)
                .ThenInclude(l => l!.Property)
            .FirstOrDefaultAsync(b => b.LeaseBillId == billId);
    }

    public async Task<IEnumerable<LeaseBill>> GetByTenantIdAsync(Guid tenantId)
    {
        return await _dbSet
            .Include(b => b.Lease)
                .ThenInclude(l => l!.Property)
            .AsNoTracking()
            .Where(b => b.Lease != null && b.Lease.TenantId == tenantId)
            .OrderByDescending(b => b.DueDate)
            .ToListAsync();
    }
}
