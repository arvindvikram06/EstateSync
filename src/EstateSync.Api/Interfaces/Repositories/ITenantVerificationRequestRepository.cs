using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using EstateSync.Api.Models;

namespace EstateSync.Api.Repositories.Interfaces;

public interface ITenantVerificationRequestRepository : IRepository<TenantVerificationRequest>
{
    Task<TenantVerificationRequest?> GetWithDetailsByIdAsync(Guid id);
    Task<TenantVerificationRequest?> GetByApplicationIdAsync(Guid applicationId);
    Task<(IEnumerable<TenantVerificationRequest> Items, int Total)> GetPagedByOwnerIdAsync(Guid ownerId, string? status, int page, int limit);
    Task<(IEnumerable<TenantVerificationRequest> Items, int Total)> GetPagedByTenantIdAsync(Guid tenantId, string? status, int page, int limit);
}
