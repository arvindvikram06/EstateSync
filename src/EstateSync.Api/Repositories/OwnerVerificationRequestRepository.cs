using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using EstateSync.Api.Data;
using EstateSync.Api.Repositories.Interfaces;
using EstateSync.Api.Models;

namespace EstateSync.Api.Repositories;

public class OwnerVerificationRequestRepository : BaseRepository<OwnerVerificationRequest>, IOwnerVerificationRequestRepository
{
    public OwnerVerificationRequestRepository(AppDbContext context) : base(context)
    {
    }

    public async Task<OwnerVerificationRequest?> GetWithDetailsByIdAsync(Guid id)
    {
        return await _dbSet
            .Include(r => r.User)
            .Include(r => r.OwnerVerificationDocuments)
                .ThenInclude(d => d.Document)
            .FirstOrDefaultAsync(r => r.RequestId == id);
    }

    public async Task<IEnumerable<OwnerVerificationRequest>> GetByUserIdAsync(Guid userId)
    {
        return await _dbSet
            .AsNoTracking()
            .Include(r => r.OwnerVerificationDocuments)
                .ThenInclude(d => d.Document)
            .Where(r => r.UserId == userId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();
    }

    public async Task<(IEnumerable<OwnerVerificationRequest> Items, int Total)> GetPagedRequestsAsync(string? status, int page, int limit)
    {
        var query = _dbSet.AsNoTracking()
            .Include(r => r.User)
            .Include(r => r.OwnerVerificationDocuments)
                .ThenInclude(d => d.Document)
            .AsQueryable();

        if (!string.IsNullOrEmpty(status) && Enum.TryParse<ApprovalStatus>(status, true, out var parsedStatus))
        {
            query = query.Where(r => r.Status == parsedStatus);
        }

        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(r => r.CreatedAt)
            .Skip((page - 1) * limit)
            .Take(limit)
            .ToListAsync();

        return (items, total);
    }
}
