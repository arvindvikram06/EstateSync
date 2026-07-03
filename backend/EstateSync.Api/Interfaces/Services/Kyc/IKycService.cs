using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using EstateSync.Api.Common;
using EstateSync.Api.DTOs.Kyc;
using EstateSync.Api.DTOs.Users;
using EstateSync.Api.Models;

namespace EstateSync.Api.Interfaces.Services.Kyc;

public interface IKycService
{
    Task<KycVerificationResponse> CreateDraftAsync(Guid userId, CancellationToken ct);
    Task<DocumentResponse> UploadDocumentAsync(Guid userId, Guid kycId, IFormFile file, DocumentType documentType, CancellationToken ct);
    Task SubmitAsync(Guid userId, Guid kycId, SubmitKycRequest request, CancellationToken ct);
    Task<IEnumerable<KycVerificationResponse>> GetByUserIdAsync(Guid userId, CancellationToken ct);
    Task<PagedResponse<KycVerificationResponse>> GetPagedAsync(string? status, int page, int limit, CancellationToken ct);
    Task<KycVerificationResponse> GetByIdForAdminAsync(Guid kycId, CancellationToken ct);
    Task ReviewAsync(Guid kycId, Guid reviewerId, string status, string remarks, CancellationToken ct);
}
