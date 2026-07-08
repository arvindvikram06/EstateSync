using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using EstateSync.Api.Interfaces.Services.Documents;
using EstateSync.Api.Interfaces.Services.Storage;
using EstateSync.Api.Models;
using EstateSync.Api.Repositories.Interfaces;

namespace EstateSync.Api.Services.Documents;

/// <summary>
/// Shared save-or-replace logic for verification document uploads —
/// previously duplicated in KycService and TenantFinancialProfileService.
/// </summary>
public class DocumentUploadService : IDocumentUploadService
{
    private readonly IFileStorageService _fileStorageService;
    private readonly IDocumentRepository _documentRepo;

    public DocumentUploadService(IFileStorageService fileStorageService, IDocumentRepository documentRepo)
    {
        _fileStorageService = fileStorageService ?? throw new ArgumentNullException(nameof(fileStorageService));
        _documentRepo = documentRepo ?? throw new ArgumentNullException(nameof(documentRepo));
    }

    public async Task<Document> SaveOrReplaceAsync(
        Document? existing, IFormFile file, DocumentType documentType,
        Guid uploadedById, string folder, CancellationToken ct)
    {
        if (existing != null)
        {
            if (!string.IsNullOrEmpty(existing.DocumentUrl))
                await _fileStorageService.DeleteFileAsync(existing.DocumentUrl);

            existing.DocumentUrl = await _fileStorageService.SaveFileAsync(file, folder, ct);
            existing.OriginalFileName = file.FileName;
            existing.MimeType = file.ContentType;
            existing.FileSize = file.Length;
            existing.UploadedAt = DateTime.UtcNow;
            existing.ReviewStatus = ApprovalStatus.Pending;
            existing.UpdatedAt = DateTime.UtcNow;
            return existing;
        }

        var filePath = await _fileStorageService.SaveFileAsync(file, folder, ct);
        var doc = new Document
        {
            DocumentId = Guid.NewGuid(),
            DocumentType = documentType,
            DocumentUrl = filePath,
            OriginalFileName = file.FileName,
            MimeType = file.ContentType,
            FileSize = file.Length,
            UploadedById = uploadedById,
            UploadedAt = DateTime.UtcNow,
            ReviewStatus = ApprovalStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };

        await _documentRepo.AddAsync(doc);
        return doc;
    }
}
