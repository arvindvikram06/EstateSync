using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
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
            .Include(c => c.ComplaintImages)
            .Where(c => c.LeaseId == leaseId)
            .ToListAsync();
    }

    public async Task<Complaint?> GetWithDetailsByIdAsync(Guid id)
    {
        return await _dbSet
            .Include(c => c.ComplaintImages)
            .Include(c => c.Lease)
                .ThenInclude(l => l!.Property)
            .FirstOrDefaultAsync(c => c.ComplaintId == id);
    }

    public async Task<IEnumerable<Complaint>> GetByTenantIdAsync(Guid tenantId)
    {
        return await _dbSet
            .AsNoTracking()
            .Include(c => c.ComplaintImages)
            .Include(c => c.Lease)
                .ThenInclude(l => l!.Property)
            .Where(c => c.Lease != null && c.Lease.TenantId == tenantId)
            .ToListAsync();
    }

    public async Task<IEnumerable<Complaint>> GetByOwnerIdAsync(Guid ownerId)
    {
        return await _dbSet
            .AsNoTracking()
            .Include(c => c.ComplaintImages)
            .Include(c => c.Lease)
                .ThenInclude(l => l!.Property)
            .Where(c => c.Lease != null && c.Lease.Property != null && c.Lease.Property.OwnerId == ownerId)
            .ToListAsync();
    }

    public async Task<IEnumerable<Complaint>> GetAllAsync()
    {
        return await _dbSet
            .AsNoTracking()
            .Include(c => c.ComplaintImages)
            .Include(c => c.Lease)
                .ThenInclude(l => l!.Property)
            .ToListAsync();
    }

    public async Task<(IEnumerable<Complaint> Items, int Total)> GetPagedForAdminAsync(
        string? status, string? priority, int page, int limit, CancellationToken ct = default)
    {
        var query = _dbSet
            .AsNoTracking()
            .Include(c => c.ComplaintImages)
            .Include(c => c.Lease)
                .ThenInclude(l => l!.Property)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<ComplaintStatus>(status, true, out var parsedStatus))
            query = query.Where(c => c.Status == parsedStatus);
        if (!string.IsNullOrWhiteSpace(priority) && Enum.TryParse<ComplaintPriority>(priority, true, out var parsedPriority))
            query = query.Where(c => c.Priority == parsedPriority);

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(c => c.CreatedAt)
            .Skip((page - 1) * limit)
            .Take(limit)
            .ToListAsync(ct);

        return (items, total);
    }

    // Reserved for background escalation job — not yet implemented
    // public async Task<IEnumerable<Complaint>> GetOpenComplaintsOlderThanDaysAsync(int days)
    // {
    //     var thresholdDate = DateTime.UtcNow.AddDays(-days);
    //     return await _dbSet
    //         .AsNoTracking()
    //         .Where(c => c.Status == ComplaintStatus.Open && c.CreatedAt < thresholdDate)
    //         .ToListAsync();
    // }
}