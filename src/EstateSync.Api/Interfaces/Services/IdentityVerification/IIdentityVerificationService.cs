using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using EstateSync.Api.Common;
using EstateSync.Api.DTOs.Users;
using EstateSync.Api.DTOs.IdentityVerification;
using EstateSync.Api.Models;

namespace EstateSync.Api.Interfaces.Services.IdentityVerification;

public interface IIdentityVerificationService
{
    Task<IdentityVerificationResponse> CreateDraftIdentityRequestAsync(Guid userId, CancellationToken ct);
    Task<DocumentResponse> UploadIdentityDocumentAsync(Guid userId, Guid requestId, IFormFile file, DocumentType documentType, CancellationToken ct);
    Task SubmitIdentityRequestAsync(Guid userId, Guid requestId, SubmitIdentityVerificationRequest request, CancellationToken ct);
    Task<IEnumerable<IdentityVerificationResponse>> GetIdentityRequestsByUserIdAsync(Guid userId, CancellationToken ct);
    Task<PagedResponse<IdentityVerificationResponse>> GetPagedIdentityRequestsAsync(string? status, int page, int limit, CancellationToken ct);
    Task VerifyIdentityRequestAsync(Guid id, Guid adminId, string status, string remarks, CancellationToken ct);
}
