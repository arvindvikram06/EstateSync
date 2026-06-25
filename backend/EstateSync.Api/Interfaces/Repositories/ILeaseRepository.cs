using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using EstateSync.Api.Models;

namespace EstateSync.Api.Repositories.Interfaces;

public interface ILeaseRepository : IRepository<Lease>
{
    Task<Lease?> GetWithDetailsByIdAsync(Guid id);
    Task<Lease?> GetActiveLeaseByPropertyIdAsync(Guid propertyId);
    Task<(IEnumerable<Lease> Items, int Total)> GetByTenantIdPagedAsync(Guid tenantId, int page, int limit);
    Task<(IEnumerable<Lease> Items, int Total)> GetByOwnerIdPagedAsync(Guid ownerId, int page, int limit);
    Task<IEnumerable<Lease>> GetActiveLeasesWithBillsAsync();
    Task<Lease?> GetActiveLeaseByTenantIdAsync(Guid tenantId);
    Task<IEnumerable<Lease>> GetActiveLeasesEndingBeforeAsync(DateTime targetDate);
    Task<Lease?> GetByApplicationIdAsync(Guid applicationId);
    Task<IEnumerable<Lease>> GetActiveLeasesPastEndDateAsync(DateTime now);

    // Reserved for future modules (e.g. Charges, Reports) — not yet called by any service
    // Task<IEnumerable<Lease>> GetByPropertyIdAsync(Guid propertyId);
    // Task<IEnumerable<Lease>> GetByTenantIdAsync(Guid tenantId);
    // Task<IEnumerable<Lease>> GetByOwnerIdAsync(Guid ownerId);
}
