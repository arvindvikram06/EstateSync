using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using EstateSync.Api.Common;
using EstateSync.Api.DTOs.Complaints;

namespace EstateSync.Api.Interfaces.Services.Complaints;

public interface IComplaintService
{
    Task<ComplaintResponse> CreateComplaintAsync(Guid tenantId, CreateComplaintRequest request, CancellationToken ct = default);
    Task<ComplaintResponse> UpdateComplaintStatusAsync(Guid userId, Guid complaintId, UpdateComplaintStatusRequest request, CancellationToken ct = default);
    Task<ComplaintResponse> GetByIdAsync(Guid complaintId, Guid userId, CancellationToken ct = default);
    Task<IEnumerable<ComplaintResponse>> GetTenantComplaintsAsync(Guid tenantId, CancellationToken ct = default);
    Task<IEnumerable<ComplaintResponse>> GetOwnerComplaintsAsync(Guid ownerId, CancellationToken ct = default);
    Task<ComplaintResponse> UploadComplaintImageAsync(Guid tenantId, Guid complaintId, IFormFile file, CancellationToken ct = default);
    Task<PagedResponse<ComplaintResponse>> GetPagedForAdminAsync(string? status, string? priority, int page, int limit, CancellationToken ct = default);
    Task MarkOwnerViewedAsync(Guid ownerId, Guid complaintId, CancellationToken ct = default);
    Task<IReadOnlyCollection<ComplaintEscalationResponse>> GetUnattendedForAdminAsync(CancellationToken ct = default);
    Task RecordAdminContactAsync(Guid adminId, Guid complaintId, RecordComplaintContactRequest request, CancellationToken ct = default);
}
