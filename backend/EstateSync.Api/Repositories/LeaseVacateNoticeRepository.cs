using System;
using System.Collections.Generic;
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

    public async Task<IEnumerable<LeaseVacateNotice>> GetDueForTerminationAsync(DateTime asOf)
        => await _dbSet
            .Include(n => n.Lease)
                .ThenInclude(l => l!.Property)
            .Include(n => n.Lease)
                .ThenInclude(l => l!.RentalApplication)
            .Where(n => n.RequestedExitDate.Date <= asOf.Date && (
                (n.Status == VacateNoticeStatus.Pending && !n.PenaltyApplied) ||
                 n.Status == VacateNoticeStatus.Accepted))
            .ToListAsync();
}
