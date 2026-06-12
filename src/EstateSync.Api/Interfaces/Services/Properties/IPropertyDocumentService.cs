using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using EstateSync.Api.DTOs.Users;
using EstateSync.Api.Models;

namespace EstateSync.Api.Interfaces.Services.Properties;

public interface IPropertyDocumentService
{
    Task<DocumentResponse> UploadDocumentAsync(Guid propertyId, IFormFile file, DocumentType documentType, Guid ownerId, CancellationToken ct);
    Task DeleteDocumentAsync(Guid propertyId, Guid documentId, Guid ownerId, CancellationToken ct);
    Task<List<DocumentResponse>> GetPropertyDocumentsAsync(Guid propertyId, Guid userId, bool isAdmin, CancellationToken ct);
}
