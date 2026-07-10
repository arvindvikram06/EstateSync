using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using EstateSync.Api.Data;
using EstateSync.Api.Models;
using EstateSync.Api.Repositories.Interfaces;

namespace EstateSync.Api.Repositories;

public class LeaseVacateNoticeRepository : BaseRepository<LeaseVacateNotice>, ILeaseVacateNoticeRepository
{
    public LeaseVacateNoticeRepository(AppDbContext context) : base(context) { }

    public async Task<LeaseVacateNotice?> GetPendingByLeaseIdAsync(Guid leaseId)
    {
        return await _dbSet
            .Include(v => v.Tenant)
            .FirstOrDefaultAsync(v => v.LeaseId == leaseId && v.Status == VacateNoticeStatus.Pending);
    }

    public async Task<LeaseVacateNotice?> GetByIdWithDetailsAsync(Guid vacateNoticeId)
    {
        return await _dbSet
            .Include(v => v.Lease)
                .ThenInclude(l => l!.Property)
            .Include(v => v.Tenant)
            .FirstOrDefaultAsync(v => v.VacateNoticeId == vacateNoticeId);
    }
}
