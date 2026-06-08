using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using EstateSync.Api.Models;

namespace EstateSync.Api.Repositories.Interfaces;

public interface IRentalApplicationRepository : IRepository<RentalApplication>
{
    Task<IEnumerable<RentalApplication>> GetByPropertyIdAsync(Guid propertyId);
    Task<IEnumerable<RentalApplication>> GetByTenantIdAsync(Guid tenantId);
    Task<bool> HasTenantAppliedAsync(Guid propertyId, Guid tenantId);
    Task<RentalApplication?> GetWithDetailsByIdAsync(Guid applicationId);
    Task<IEnumerable<RentalApplication>> GetByOwnerIdAsync(Guid ownerId);
    Task<IEnumerable<RentalApplication>> GetByPropertyIdWithDetailsAsync(Guid propertyId);
}
