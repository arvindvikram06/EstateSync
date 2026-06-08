using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using EstateSync.Api.Models;

namespace EstateSync.Api.Repositories.Interfaces;

public interface ILeaseRepository : IRepository<Lease>
{
    Task<Lease?> GetWithDetailsByIdAsync(Guid id);
    Task<IEnumerable<Lease>> GetByPropertyIdAsync(Guid propertyId);
    Task<Lease?> GetActiveLeaseByPropertyIdAsync(Guid propertyId);
    Task<IEnumerable<Lease>> GetByTenantIdAsync(Guid tenantId);
    Task<IEnumerable<Lease>> GetByOwnerIdAsync(Guid ownerId);
}
