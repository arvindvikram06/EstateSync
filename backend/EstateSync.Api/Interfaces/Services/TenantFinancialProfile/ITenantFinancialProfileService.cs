using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using EstateSync.Api.Common;
using EstateSync.Api.DTOs.TenantFinancialProfile;
using EstateSync.Api.DTOs.Users;
using EstateSync.Api.Models;

namespace EstateSync.Api.Interfaces.Services.TenantFinancialProfile;

public interface ITenantFinancialProfileService
{
    Task<TenantFinancialProfileResponse> CreateDraftAsync(Guid tenantId, CancellationToken ct);
    Task<DocumentResponse> UploadDocumentAsync(Guid tenantId, Guid profileId, IFormFile file, DocumentType documentType, CancellationToken ct);
    Task SubmitAsync(Guid tenantId, Guid profileId, SubmitTenantFinancialProfileRequest request, CancellationToken ct);
    Task<IEnumerable<TenantFinancialProfileResponse>> GetByTenantIdAsync(Guid tenantId, CancellationToken ct);
    Task<PagedResponse<TenantFinancialProfileResponse>> GetPagedAsync(string? status, int page, int limit, CancellationToken ct);
    Task ReviewAsync(Guid profileId, Guid reviewerId, string status, string remarks, CancellationToken ct);
}
