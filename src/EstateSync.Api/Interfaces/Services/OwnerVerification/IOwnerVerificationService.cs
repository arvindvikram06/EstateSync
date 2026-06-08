using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using EstateSync.Api.Common;
using EstateSync.Api.DTOs.Users;
using EstateSync.Api.Models;

namespace EstateSync.Api.Interfaces.Services.OwnerVerification;

public interface IOwnerVerificationService
{
    Task<Guid> CreateDraftVerificationRequestAsync(Guid userId, CancellationToken ct);
    Task<DocumentResponse> UploadVerificationDocumentAsync(Guid userId, Guid requestId, IFormFile file, DocumentType documentType, CancellationToken ct);
    Task SubmitVerificationRequestAsync(Guid userId, Guid requestId, CancellationToken ct);
    Task<IEnumerable<VerificationRequestResponse>> GetUserVerificationRequestsAsync(Guid userId, CancellationToken ct);
    Task<PagedResponse<VerificationRequestResponse>> GetAdminVerificationRequestsAsync(string? status, int page, int limit, CancellationToken ct);
    Task VerifyRequestAsync(Guid id, Guid adminId, string status, string remarks, CancellationToken ct);
}
