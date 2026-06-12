using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EstateSync.Api.Common;
using EstateSync.Api.DTOs.Leases;

namespace EstateSync.Api.Interfaces.Services.Leases;

public interface ILeaseService
{
    Task<LeaseResponse> CreateLeaseAsync(CreateLeaseRequest request, Guid ownerId, CancellationToken ct);
    Task<LeaseResponse> SignLeaseAsync(Guid leaseId, SignLeaseRequest request, Guid userId, CancellationToken ct);
    Task<LeaseResponse> GetLeaseByIdAsync(Guid leaseId, Guid userId, bool isAdmin, CancellationToken ct);
    Task<PagedResponse<LeaseResponse>> GetTenantLeasesAsync(Guid tenantId, int page, int limit, CancellationToken ct);
    Task<PagedResponse<LeaseResponse>> GetOwnerLeasesAsync(Guid ownerId, int page, int limit, CancellationToken ct);
    Task<PagedResponse<LeaseResponse>> GetAllLeasesAsync(int page, int limit, CancellationToken ct);
}
