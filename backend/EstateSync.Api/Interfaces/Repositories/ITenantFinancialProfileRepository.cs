using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using EstateSync.Api.Models;

namespace EstateSync.Api.Repositories.Interfaces;

public interface ITenantFinancialProfileRepository : IRepository<TenantFinancialProfile>
{
    Task<TenantFinancialProfile?> GetWithDetailsByIdAsync(Guid id);
    Task<TenantFinancialProfile?> GetApprovedByTenantIdAsync(Guid tenantId);
    Task<IEnumerable<TenantFinancialProfile>> GetByTenantIdAsync(Guid tenantId);
    Task<(IEnumerable<TenantFinancialProfile> Items, int Total)> GetPagedForListAsync(string? status, int page, int limit);
    Task<(IEnumerable<TenantFinancialProfile> Items, int Total)> GetPagedAsync(string? status, int page, int limit);
}
