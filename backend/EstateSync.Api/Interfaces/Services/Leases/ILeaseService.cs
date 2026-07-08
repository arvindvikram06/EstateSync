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
    Task<LeaseResponse> UpdateLeaseAsync(Guid leaseId, UpdateLeaseRequest request, Guid ownerId, CancellationToken ct);
    Task<LeaseResponse> SignLeaseAsync(Guid leaseId, SignLeaseRequest request, Guid userId, CancellationToken ct);
    Task VoidLeaseAsync(Guid leaseId, string? reason, Guid ownerId, CancellationToken ct);
    Task AdminApproveLeaseAsync(Guid leaseId, bool approve, string? remarks, Guid adminId, CancellationToken ct);
    Task<LeaseResponse> GetLeaseByIdAsync(Guid leaseId, Guid userId, bool isAdmin, CancellationToken ct);
    Task<List<LeaseBillDetailResponse>> GetLeaseBillsAsync(Guid leaseId, Guid userId, bool isAdmin, CancellationToken ct);
    Task<List<PaymentRecordResponse>> GetLeasePaymentsAsync(Guid leaseId, Guid userId, bool isAdmin, CancellationToken ct);
    Task<PagedResponse<LeaseResponse>> GetTenantLeasesAsync(Guid tenantId, int page, int limit, CancellationToken ct);
    Task<PagedResponse<LeaseResponse>> GetOwnerLeasesAsync(Guid ownerId, int page, int limit, CancellationToken ct);
    Task<PagedResponse<LeaseResponse>> GetAllLeasesAsync(int page, int limit, CancellationToken ct);
    Task RequestRenewalAsync(Guid leaseId, Guid tenantId, CancellationToken ct);
    Task ApproveRenewalAsync(Guid leaseId, Guid ownerId, DateTime newEndDate, CancellationToken ct);
    Task RejectRenewalAsync(Guid leaseId, Guid ownerId, CancellationToken ct);
    Task TerminateLeaseAsync(Guid leaseId, Guid requesterId, bool isAdmin, string reason, bool forfeitDeposit, CancellationToken ct);
}
