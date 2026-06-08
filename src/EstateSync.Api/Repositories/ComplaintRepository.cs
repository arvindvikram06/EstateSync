using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using EstateSync.Api.Data;
using EstateSync.Api.Repositories.Interfaces;
using EstateSync.Api.Models;

namespace EstateSync.Api.Repositories;

public class ComplaintRepository : BaseRepository<Complaint>, IComplaintRepository
{
    public ComplaintRepository(AppDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<Complaint>> GetByLeaseIdAsync(Guid leaseId)
    {
        return await _dbSet
            .AsNoTracking()
            .Where(c => c.LeaseId == leaseId)
            .ToListAsync();
    }

    public async Task<IEnumerable<Complaint>> GetOpenComplaintsOlderThanDaysAsync(int days)
    {
        var thresholdDate = DateTime.UtcNow.AddDays(-days);
        return await _dbSet
            .AsNoTracking()
            .Where(c => c.Status == ComplaintStatus.Open && c.CreatedAt < thresholdDate)
            .ToListAsync();
    }
}