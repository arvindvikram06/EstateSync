using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using EstateSync.Api.Data;
using EstateSync.Api.Repositories.Interfaces;
using EstateSync.Api.Models;

namespace EstateSync.Api.Repositories;

public class TenantFinancialProfileRepository : BaseRepository<TenantFinancialProfile>, ITenantFinancialProfileRepository
{
    public TenantFinancialProfileRepository(AppDbContext context) : base(context)
    {
    }

    public async Task<TenantFinancialProfile?> GetWithDetailsByIdAsync(Guid id)
    {
        return await _dbSet
            .Include(p => p.Tenant)
            .Include(p => p.TenantFinancialDocuments)
                .ThenInclude(d => d.Document)
            .FirstOrDefaultAsync(p => p.ProfileId == id);
    }

    public async Task<TenantFinancialProfile?> GetApprovedByTenantIdAsync(Guid tenantId)
    {
        return await _dbSet
            .AsNoTracking()
            .Where(p => p.TenantId == tenantId && p.Status == ApprovalStatus.Approved)
            .OrderByDescending(p => p.CreatedAt)
            .FirstOrDefaultAsync();
    }

    public async Task<IEnumerable<TenantFinancialProfile>> GetByTenantIdAsync(Guid tenantId)
    {
        return await _dbSet
            .AsNoTracking()
            .Include(p => p.TenantFinancialDocuments)
                .ThenInclude(d => d.Document)
            .Where(p => p.TenantId == tenantId)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();
    }

    public async Task<(IEnumerable<TenantFinancialProfile> Items, int Total)> GetPagedForListAsync(string? status, int page, int limit)
    {
        var query = _dbSet.AsNoTracking()
            .Include(p => p.Tenant)
            .AsQueryable();

        if (!string.IsNullOrEmpty(status) && Enum.TryParse<ApprovalStatus>(status, true, out var parsedStatus))
        {
            query = query.Where(p => p.Status == parsedStatus);
        }

        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(p => p.CreatedAt)
            .Skip((page - 1) * limit)
            .Take(limit)
            .ToListAsync();

        return (items, total);
    }

    public async Task<(IEnumerable<TenantFinancialProfile> Items, int Total)> GetPagedAsync(string? status, int page, int limit)
    {
        var query = _dbSet.AsNoTracking()
            .Include(p => p.Tenant)
            .Include(p => p.TenantFinancialDocuments)
                .ThenInclude(d => d.Document)
            .AsQueryable();

        if (!string.IsNullOrEmpty(status) && Enum.TryParse<ApprovalStatus>(status, true, out var parsedStatus))
        {
            query = query.Where(p => p.Status == parsedStatus);
        }

        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(p => p.CreatedAt)
            .Skip((page - 1) * limit)
            .Take(limit)
            .ToListAsync();

        return (items, total);
    }
}
