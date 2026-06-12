using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using EstateSync.Api.Data;
using EstateSync.Api.Repositories.Interfaces;
using EstateSync.Api.Models;

namespace EstateSync.Api.Repositories;

public class TenantVerificationRequestRepository : BaseRepository<TenantVerificationRequest>, ITenantVerificationRequestRepository
{
    public TenantVerificationRequestRepository(AppDbContext context) : base(context)
    {
    }

    public async Task<TenantVerificationRequest?> GetWithDetailsByIdAsync(Guid id)
    {
        return await _dbSet
            .Include(r => r.Tenant)
            .Include(r => r.RentalApplication)
                .ThenInclude(a => a!.Property)
            .Include(r => r.TenantVerificationDocuments)
                .ThenInclude(d => d.Document)
            .FirstOrDefaultAsync(r => r.RequestId == id);
    }

    public async Task<TenantVerificationRequest?> GetByApplicationIdAsync(Guid applicationId)
    {
        return await _dbSet
            .Include(r => r.Tenant)
            .Include(r => r.TenantVerificationDocuments)
                .ThenInclude(d => d.Document)
            .FirstOrDefaultAsync(r => r.ApplicationId == applicationId);
    }

    public async Task<(IEnumerable<TenantVerificationRequest> Items, int Total)> GetPagedByOwnerIdAsync(Guid ownerId, string? status, int page, int limit)
    {
        var query = _dbSet
            .AsNoTracking()
            .Include(r => r.Tenant)
            .Include(r => r.RentalApplication)
                .ThenInclude(a => a!.Property)
            .Include(r => r.TenantVerificationDocuments)
                .ThenInclude(d => d.Document)
            .Where(r => r.RentalApplication!.Property!.OwnerId == ownerId);

        if (!string.IsNullOrEmpty(status))
        {
            if (Enum.TryParse<ApprovalStatus>(status, true, out var approvalStatus))
            {
                query = query.Where(r => r.Status == approvalStatus);
            }
        }

        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(r => r.CreatedAt)
            .Skip((page - 1) * limit)
            .Take(limit)
            .ToListAsync();

        return (items, total);
    }

    public async Task<(IEnumerable<TenantVerificationRequest> Items, int Total)> GetPagedByTenantIdAsync(Guid tenantId, string? status, int page, int limit)
    {
        var query = _dbSet
            .AsNoTracking()
            .Include(r => r.Tenant)
            .Include(r => r.TenantVerificationDocuments)
                .ThenInclude(d => d.Document)
            .Where(r => r.TenantId == tenantId);

        if (!string.IsNullOrEmpty(status))
        {
            if (Enum.TryParse<ApprovalStatus>(status, true, out var approvalStatus))
            {
                query = query.Where(r => r.Status == approvalStatus);
            }
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
