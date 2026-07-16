using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using EstateSync.Api.Models;

namespace EstateSync.Api.Repositories.Interfaces;

public interface IRentalApplicationRepository : IRepository<RentalApplication>
{
    Task<RentalApplication?> GetWithDetailsByIdAsync(Guid applicationId);
    Task<bool> HasTenantAppliedAsync(Guid propertyId, Guid tenantId);
    Task<RentalApplication?> GetRejectedOrWithdrawnAsync(Guid propertyId, Guid tenantId);
    /// <summary>Tracked query — callers mutate the returned entities and rely on SaveChangesAsync to persist.</summary>
    Task<IEnumerable<RentalApplication>> GetByPropertyIdForUpdateAsync(Guid propertyId);
    Task<(IEnumerable<RentalApplication> Items, int Total)> GetByOwnerIdPagedAsync(Guid ownerId, int page, int limit);
    Task<(IEnumerable<RentalApplication> Items, int Total)> GetByPropertyIdWithDetailsPagedAsync(Guid propertyId, int page, int limit);
    Task<(IEnumerable<RentalApplication> Items, int Total)> GetByTenantIdWithDetailsPagedAsync(Guid tenantId, int page, int limit);

    // Reserved for future modules — not yet called by any service
    // Task<IEnumerable<RentalApplication>> GetByTenantIdAsync(Guid tenantId);
}

