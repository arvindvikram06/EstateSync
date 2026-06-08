using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EstateSync.Api.DTOs.RentalApplications;

namespace EstateSync.Api.Interfaces.Services.RentalApplications;

public interface IRentalApplicationService
{
    Task<RentalApplicationResponse> CreateApplicationAsync(Guid propertyId, CreateRentalApplicationRequest request, Guid tenantId, CancellationToken ct);
    Task<RentalApplicationResponse> ReviewApplicationAsync(Guid applicationId, bool approve, string remarks, Guid ownerId, CancellationToken ct);
    Task<IEnumerable<RentalApplicationResponse>> GetOwnerApplicationsAsync(Guid ownerId, CancellationToken ct);
    Task<IEnumerable<RentalApplicationResponse>> GetApplicationsByPropertyIdAsync(Guid propertyId, Guid ownerId, CancellationToken ct);
}
