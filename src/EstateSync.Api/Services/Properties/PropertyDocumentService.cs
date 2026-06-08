using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using EstateSync.Api.DTOs.Users;
using EstateSync.Api.Exceptions;
using EstateSync.Api.Interfaces.Services.Properties;
using EstateSync.Api.Interfaces.Services.Storage;
using EstateSync.Api.Mappers;
using EstateSync.Api.Models;
using EstateSync.Api.Repositories.Interfaces;

namespace EstateSync.Api.Services.Properties;

public class PropertyDocumentService : IPropertyDocumentService
{
    private readonly IPropertyRepository _propertyRepo;
    private readonly IDocumentRepository _documentRepo;
    private readonly IFileStorageService _fileStorageService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly UserMapper _mapper;
    private readonly ILogger<PropertyDocumentService> _logger;

    public PropertyDocumentService(
        IPropertyRepository propertyRepo,
        IDocumentRepository documentRepo,
        IFileStorageService fileStorageService,
        IUnitOfWork unitOfWork,
        UserMapper mapper,
        ILogger<PropertyDocumentService> logger)
    {
        _propertyRepo = propertyRepo ?? throw new ArgumentNullException(nameof(propertyRepo));
        _documentRepo = documentRepo ?? throw new ArgumentNullException(nameof(documentRepo));
        _fileStorageService = fileStorageService ?? throw new ArgumentNullException(nameof(fileStorageService));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<DocumentResponse> UploadDocumentAsync(Guid propertyId, IFormFile file, DocumentType documentType, Guid ownerId, CancellationToken ct)
    {
        var property = await _propertyRepo.GetWithDetailsByIdAsync(propertyId);
        if (property == null)
        {
            throw new NotFoundException("Property", propertyId);
        }

        if (property.OwnerId != ownerId)
        {
            throw new ForbiddenException("You do not own this property.");
        }

        if (property.Status != PropertyStatus.Draft && property.Status != PropertyStatus.Rejected)
        {
            throw new BadRequestException("Documents can only be uploaded to properties in Draft or Rejected status.");
        }

        if (file == null || file.Length == 0)
        {
            throw new BadRequestException("File is empty.");
        }

        if (file.Length > 10 * 1024 * 1024)
        {
            throw new BadRequestException("File size exceeds 10MB limit.");
        }

        var allowedExtensions = new[] { ".pdf", ".jpg", ".jpeg", ".png" };
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!allowedExtensions.Contains(extension))
        {
            throw new BadRequestException("Only PDF, JPG, JPEG, and PNG files are allowed.");
        }

        var fileUrl = await _fileStorageService.SaveFileAsync(file, $"properties/{propertyId}/documents", ct);

        var existingLink = property.PropertyDocuments
            .FirstOrDefault(pd => pd.Document?.DocumentType == documentType);

        Document doc;

        if (existingLink != null && existingLink.Document != null)
        {
            doc = existingLink.Document;
            await _fileStorageService.DeleteFileAsync(doc.DocumentUrl);

            doc.DocumentUrl = fileUrl;
            doc.OriginalFileName = file.FileName;
            doc.MimeType = file.ContentType;
            doc.FileSize = file.Length;
            doc.UploadedAt = DateTime.UtcNow;
            doc.ReviewStatus = ApprovalStatus.Pending;
            doc.ReviewNotes = null;

            _logger.LogInformation("Document {DocumentId} of type {DocType} updated for property {PropertyId} by owner {OwnerId}", doc.DocumentId, documentType, propertyId, ownerId);
        }
        else
        {
            doc = new Document
            {
                DocumentId = Guid.NewGuid(),
                DocumentType = documentType,
                DocumentUrl = fileUrl,
                OriginalFileName = file.FileName,
                MimeType = file.ContentType,
                FileSize = file.Length,
                UploadedById = ownerId,
                UploadedAt = DateTime.UtcNow,
                ReviewStatus = ApprovalStatus.Pending,
                CreatedAt = DateTime.UtcNow
            };

            await _documentRepo.AddAsync(doc);

            var link = new PropertyDocument
            {
                PropertyId = propertyId,
                DocumentId = doc.DocumentId,
                Document = doc,
                CreatedAt = DateTime.UtcNow
            };

            property.PropertyDocuments.Add(link);

            _logger.LogInformation("Document {DocumentId} of type {DocType} uploaded and linked to property {PropertyId} by owner {OwnerId}", doc.DocumentId, documentType, propertyId, ownerId);
        }

        await _propertyRepo.UpdateAsync(property);
        await _unitOfWork.SaveChangesAsync(ct);

        return _mapper.MapToDocumentResponse(doc);
    }

    public async Task DeleteDocumentAsync(Guid propertyId, Guid documentId, Guid ownerId, CancellationToken ct)
    {
        var property = await _propertyRepo.GetWithDetailsByIdAsync(propertyId);
        if (property == null)
        {
            throw new NotFoundException("Property", propertyId);
        }

        if (property.OwnerId != ownerId)
        {
            throw new ForbiddenException("You do not own this property.");
        }

        if (property.Status != PropertyStatus.Draft && property.Status != PropertyStatus.Rejected)
        {
            throw new BadRequestException("Documents can only be deleted from properties in Draft or Rejected status.");
        }

        var link = property.PropertyDocuments.FirstOrDefault(pd => pd.DocumentId == documentId);
        if (link == null || link.Document == null)
        {
            throw new NotFoundException("PropertyDocument", documentId);
        }

        await _fileStorageService.DeleteFileAsync(link.Document.DocumentUrl);
        await _documentRepo.DeleteAsync(link.Document);
        property.PropertyDocuments.Remove(link);

        _logger.LogInformation("Document {DocumentId} deleted from property {PropertyId} by owner {OwnerId}", documentId, propertyId, ownerId);

        await _propertyRepo.UpdateAsync(property);
        await _unitOfWork.SaveChangesAsync(ct);
    }
}
