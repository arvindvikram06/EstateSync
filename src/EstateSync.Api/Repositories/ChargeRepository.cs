using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using EstateSync.Api.Data;
using EstateSync.Api.Repositories.Interfaces;
using EstateSync.Api.Models;

namespace EstateSync.Api.Repositories;

public class ChargeRepository : BaseRepository<Charge>, IChargeRepository
{
    public ChargeRepository(AppDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<Charge>> GetByLeaseIdAsync(Guid leaseId)
    {
        return await _dbSet
            .AsNoTracking()
            .Where(c => c.LeaseId == leaseId)
            .ToListAsync();
    }

    public async Task<IEnumerable<Charge>> GetOverdueChargesAsync(DateTime currentDate)
    {
        return await _dbSet
            .AsNoTracking()
            .Where(c => c.DueDate < currentDate && c.PendingAmount > 0)
            .ToListAsync();
    }
}