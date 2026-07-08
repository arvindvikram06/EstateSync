using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using EstateSync.Api.Models;

namespace EstateSync.Api.Interfaces.Services.Documents;

public interface IDocumentUploadService
{
    /// <summary>
    /// Replaces the file of an existing Document (deleting the old file) or
    /// creates and persists a new Document entity for the upload.
    /// Does not call SaveChangesAsync — the owning service commits.
    /// </summary>
    Task<Document> SaveOrReplaceAsync(
        Document? existing, IFormFile file, DocumentType documentType,
        Guid uploadedById, string folder, CancellationToken ct);
}
