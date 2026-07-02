using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EstateSync.Api.Models;

namespace EstateSync.Api.Repositories.Interfaces;

public interface IComplaintRepository : IRepository<Complaint>
{
    Task<IEnumerable<Complaint>> GetByLeaseIdAsync(Guid leaseId);
    Task<Complaint?> GetWithDetailsByIdAsync(Guid id);
    Task<IEnumerable<Complaint>> GetByTenantIdAsync(Guid tenantId);
    Task<IEnumerable<Complaint>> GetByOwnerIdAsync(Guid ownerId);
    Task<IEnumerable<Complaint>> GetAllAsync();
    Task<(IEnumerable<Complaint> Items, int Total)> GetPagedForAdminAsync(
        string? status, string? priority, int page, int limit, CancellationToken ct = default);

    // Reserved for background escalation job \u2014 not yet implemented
    // Task<IEnumerable<Complaint>> GetOpenComplaintsOlderThanDaysAsync(int days);
}
