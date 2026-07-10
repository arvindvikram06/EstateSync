using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EstateSync.Api.Common;
using EstateSync.Api.DTOs.RentalApplications;


namespace EstateSync.Api.Interfaces.Services.RentalApplications;

public interface IRentalApplicationService
{
    Task<RentalApplicationResponse> CreateApplicationAsync(Guid propertyId, CreateRentalApplicationRequest request, Guid tenantId, CancellationToken ct);
    Task<RentalApplicationResponse> ReviewApplicationAsync(Guid applicationId, bool approve, string remarks, Guid ownerId, CancellationToken ct);
    Task<PagedResponse<RentalApplicationResponse>> GetOwnerApplicationsAsync(Guid ownerId, int page, int limit, CancellationToken ct);
    Task<PagedResponse<RentalApplicationResponse>> GetApplicationsByPropertyIdAsync(Guid propertyId, Guid ownerId, int page, int limit, CancellationToken ct);
    Task<PagedResponse<RentalApplicationResponse>> GetTenantApplicationsAsync(Guid tenantId, int page, int limit, CancellationToken ct);
    Task<RentalApplicationResponse> GetApplicationByIdAsync(Guid applicationId, Guid userId, bool isAdmin, CancellationToken ct);
    Task DeleteApplicationAsync(Guid applicationId, Guid tenantId, CancellationToken ct);
}
