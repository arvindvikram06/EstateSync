using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EstateSync.Api.DTOs.TenantVerification;
using EstateSync.Api.DTOs.Users;
using Microsoft.AspNetCore.Http;
using EstateSync.Api.Models;
using EstateSync.Api.Common;

namespace EstateSync.Api.Interfaces.Services.TenantVerification;

public interface ITenantVerificationService
{
    Task<TenantVerificationResponse> InitiateVerificationAsync(Guid applicationId, Guid tenantId, CancellationToken ct);
    Task<DocumentResponse> UploadDocumentAsync(Guid requestId, IFormFile file, DocumentType documentType, Guid tenantId, CancellationToken ct);
    Task SubmitVerificationAsync(Guid requestId, Guid tenantId, CancellationToken ct);
    Task<TenantVerificationResponse> ReviewVerificationAsync(Guid requestId, ReviewTenantVerificationRequest request, Guid ownerId, CancellationToken ct);
    Task<TenantVerificationResponse> GetByIdAsync(Guid requestId, Guid userId, bool isAdmin, CancellationToken ct);
    Task<PagedResponse<TenantVerificationResponse>> GetPagedTenantRequestsAsync(Guid tenantId, string? status, int page, int limit, CancellationToken ct);
    Task<PagedResponse<TenantVerificationResponse>> GetPagedOwnerRequestsAsync(Guid ownerId, string? status, int page, int limit, CancellationToken ct);
}
