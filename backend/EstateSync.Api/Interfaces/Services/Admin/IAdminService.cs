using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EstateSync.Api.DTOs.Admin;

namespace EstateSync.Api.Interfaces.Services.Admin;

public interface IAdminService
{
    Task CreateVerifierAsync(CreateStaffRequest request, CancellationToken ct = default);
    Task DeactivateStaffAsync(Guid userId, CancellationToken ct = default);
    Task ApproveLeaseAsync(Guid leaseId, bool approve, string? remarks, Guid adminId, CancellationToken ct = default);
    Task<IEnumerable<StaffResponse>> GetStaffAsync(CancellationToken ct = default);
}
