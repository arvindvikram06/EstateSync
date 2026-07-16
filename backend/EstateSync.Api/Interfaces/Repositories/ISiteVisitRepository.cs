using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using EstateSync.Api.Models;

namespace EstateSync.Api.Repositories.Interfaces;

public interface ISiteVisitRepository : IRepository<SiteVisit>
{
    Task<IEnumerable<SiteVisit>> GetByTenantIdAsync(Guid tenantId);
    Task<IEnumerable<SiteVisit>> GetByOwnerIdAsync(Guid ownerId);
    Task<SiteVisit?> GetWithDetailsByIdAsync(Guid id);
    Task<bool> HasActiveRequestAsync(Guid tenantId, Guid propertyId);
}
