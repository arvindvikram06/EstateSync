using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using EstateSync.Api.Common;
using EstateSync.Api.DTOs.TenantVerification;
using EstateSync.Api.DTOs.Users;
using EstateSync.Api.Exceptions;
using EstateSync.Api.Interfaces.Services.TenantVerification;
using EstateSync.Api.Interfaces.Services.Storage;
using EstateSync.Api.Mappers;
using EstateSync.Api.Models;
using EstateSync.Api.Repositories.Interfaces;

namespace EstateSync.Api.Services.TenantVerification;

public class TenantVerificationService : ITenantVerificationService
{
    private readonly ITenantVerificationRequestRepository _requestRepo;
    private readonly IRentalApplicationRepository _applicationRepo;
    private readonly IDocumentRepository _documentRepo;
    private readonly IFileStorageService _fileStorageService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly TenantVerificationMapper _mapper;
    private readonly ILogger<TenantVerificationService> _logger;

    public TenantVerificationService(
        ITenantVerificationRequestRepository requestRepo,
        IRentalApplicationRepository applicationRepo,
        IDocumentRepository documentRepo,
        IFileStorageService fileStorageService,
        IUnitOfWork unitOfWork,
        TenantVerificationMapper mapper,
        ILogger<TenantVerificationService> logger)
    {
        _requestRepo = requestRepo ?? throw new ArgumentNullException(nameof(requestRepo));
        _applicationRepo = applicationRepo ?? throw new ArgumentNullException(nameof(applicationRepo));
        _documentRepo = documentRepo ?? throw new ArgumentNullException(nameof(documentRepo));
        _fileStorageService = fileStorageService ?? throw new ArgumentNullException(nameof(fileStorageService));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<TenantVerificationResponse> InitiateVerificationAsync(Guid applicationId, Guid tenantId, CancellationToken ct)
    {
        var application = await _applicationRepo.GetWithDetailsByIdAsync(applicationId);
        if (application == null)
        {
            throw new NotFoundException("RentalApplication", applicationId);
        }

        if (application.TenantId != tenantId)
        {
            throw new ForbiddenException("You did not submit this rental application.");
        }

        if (application.Status != ApplicationStatus.Approved)
        {
            throw new BadRequestException("Rental application is not approved.");
        }

        var existingRequest = await _requestRepo.GetByApplicationIdAsync(applicationId);
        if (existingRequest != null)
        {
            if (existingRequest.Status == ApprovalStatus.Pending || existingRequest.Status == ApprovalStatus.Approved)
            {
                throw new ConflictException("A verification request is already pending or approved for this application.");
            }

            if (existingRequest.Status == ApprovalStatus.Draft)
            {
                var detailedDraft = await _requestRepo.GetWithDetailsByIdAsync(existingRequest.RequestId);
                return _mapper.MapToResponse(detailedDraft!);
            }
        }

        var newRequest = new TenantVerificationRequest
        {
            RequestId = Guid.NewGuid(),
            TenantId = tenantId,
            ApplicationId = applicationId,
            Status = ApprovalStatus.Draft,
            CreatedAt = DateTime.UtcNow
        };

        await _requestRepo.AddAsync(newRequest);
        await _unitOfWork.SaveChangesAsync(ct);

        _logger.LogInformation("Initiated tenant verification request {RequestId} for application {ApplicationId}", newRequest.RequestId, applicationId);

        var detailedRequest = await _requestRepo.GetWithDetailsByIdAsync(newRequest.RequestId);
        return _mapper.MapToResponse(detailedRequest!);
    }

    public async Task<DocumentResponse> UploadDocumentAsync(Guid requestId, IFormFile file, DocumentType documentType, Guid tenantId, CancellationToken ct)
    {
        if (documentType != DocumentType.SalarySlip && documentType != DocumentType.BankProof)
        {
            throw new BadRequestException("Only SalarySlip and BankProof are allowed for tenant verification.");
        }

        if (file == null)
        {
            throw new BadRequestException("No file was uploaded.");
        }

        var isPdf = file.ContentType.Equals("application/pdf", StringComparison.OrdinalIgnoreCase) ||
                    Path.GetExtension(file.FileName).Equals(".pdf", StringComparison.OrdinalIgnoreCase);
        if (!isPdf)
        {
            throw new BadRequestException("Only PDF documents are allowed.");
        }

        var request = await _requestRepo.GetWithDetailsByIdAsync(requestId);
        if (request == null)
        {
            throw new NotFoundException("TenantVerificationRequest", requestId);
        }

        if (request.TenantId != tenantId)
        {
            throw new ForbiddenException("You are not authorized to upload documents for this request.");
        }

        if (request.Status != ApprovalStatus.Draft)
        {
            throw new ConflictException("You can only upload documents to a draft request.");
        }

        var document = await SaveOrUpdateDocumentAsync(request, file, documentType, tenantId, requestId, ct);

        await _unitOfWork.SaveChangesAsync(ct);

        _logger.LogInformation("Uploaded document type {DocumentType} for tenant request {RequestId}", documentType, requestId);
        return _mapper.MapToDocumentResponse(document);
    }

    public async Task SubmitVerificationAsync(Guid requestId, Guid tenantId, CancellationToken ct)
    {
        var request = await _requestRepo.GetWithDetailsByIdAsync(requestId);
        if (request == null)
        {
            throw new NotFoundException("TenantVerificationRequest", requestId);
        }

        if (request.TenantId != tenantId)
        {
            throw new ForbiddenException("You are not authorized to submit this request.");
        }

        if (request.Status != ApprovalStatus.Draft)
        {
            throw new ConflictException("Request is not in a draft state.");
        }

        var docs = request.TenantVerificationDocuments
            .Select(x => x.Document)
            .Where(x => x != null)
            .ToList();

        var required = new[] { DocumentType.SalarySlip, DocumentType.BankProof };
        var missing = required.Where(x => !docs.Any(d => d!.DocumentType == x)).ToList();

        if (missing.Any())
        {
            throw new BadRequestException($"Missing required document types: {string.Join(", ", missing)}");
        }

        request.Status = ApprovalStatus.Pending;
        await _requestRepo.UpdateAsync(request);
        await _unitOfWork.SaveChangesAsync(ct);

        _logger.LogInformation("Submitted tenant verification request {RequestId}", requestId);
    }

    public async Task<TenantVerificationResponse> ReviewVerificationAsync(Guid requestId, ReviewTenantVerificationRequest request, Guid ownerId, CancellationToken ct)
    {
        var tvRequest = await _requestRepo.GetWithDetailsByIdAsync(requestId);
        if (tvRequest == null)
        {
            throw new NotFoundException("TenantVerificationRequest", requestId);
        }

        var property = tvRequest.RentalApplication?.Property;
        if (property == null || property.OwnerId != ownerId)
        {
            throw new ForbiddenException("You do not own the property associated with this request.");
        }

        if (tvRequest.Status != ApprovalStatus.Pending)
        {
            throw new ConflictException("Request is not in a pending review state.");
        }

        tvRequest.Status = request.Approve ? ApprovalStatus.Approved : ApprovalStatus.Rejected;
        tvRequest.ReviewedById = ownerId;
        tvRequest.ReviewRemarks = request.Remarks;
        tvRequest.UpdatedAt = DateTime.UtcNow;

        await _requestRepo.UpdateAsync(tvRequest);
        await _unitOfWork.SaveChangesAsync(ct);

        _logger.LogInformation("Reviewed tenant verification request {RequestId}. Approved: {Approved}", requestId, request.Approve);

        var detailed = await _requestRepo.GetWithDetailsByIdAsync(requestId);
        return _mapper.MapToResponse(detailed!);
    }

    public async Task<TenantVerificationResponse> GetByIdAsync(Guid requestId, Guid userId, bool isAdmin, CancellationToken ct)
    {
        var tvRequest = await _requestRepo.GetWithDetailsByIdAsync(requestId);
        if (tvRequest == null)
        {
            throw new NotFoundException("TenantVerificationRequest", requestId);
        }

        var isTenant = tvRequest.TenantId == userId;
        var isOwner = tvRequest.RentalApplication?.Property != null && tvRequest.RentalApplication.Property.OwnerId == userId;

        if (!isTenant && !isOwner && !isAdmin)
        {
            throw new ForbiddenException("You are not authorized to view this request.");
        }

        return _mapper.MapToResponse(tvRequest);
    }

    public async Task<PagedResponse<TenantVerificationResponse>> GetPagedTenantRequestsAsync(Guid tenantId, string? status, int page, int limit, CancellationToken ct)
    {
        var (items, total) = await _requestRepo.GetPagedByTenantIdAsync(tenantId, status, page, limit);
        var mapped = items.Select(r => _mapper.MapToResponse(r)).ToList();

        return new PagedResponse<TenantVerificationResponse>
        {
            Success = true,
            Data = mapped,
            Page = page,
            Limit = limit,
            Total = total
        };
    }

    public async Task<PagedResponse<TenantVerificationResponse>> GetPagedOwnerRequestsAsync(Guid ownerId, string? status, int page, int limit, CancellationToken ct)
    {
        var (items, total) = await _requestRepo.GetPagedByOwnerIdAsync(ownerId, status, page, limit);
        var mapped = items.Select(r => _mapper.MapToResponse(r)).ToList();

        return new PagedResponse<TenantVerificationResponse>
        {
            Success = true,
            Data = mapped,
            Page = page,
            Limit = limit,
            Total = total
        };
    }

    private async Task<Document> SaveOrUpdateDocumentAsync(TenantVerificationRequest request, IFormFile file, DocumentType documentType, Guid tenantId, Guid requestId, CancellationToken ct)
    {
        var existingLink = request.TenantVerificationDocuments
            .FirstOrDefault(x => x.Document != null && x.Document.DocumentType == documentType);

        Document document;
        if (existingLink != null)
        {
            document = existingLink.Document!;
            if (!string.IsNullOrEmpty(document.DocumentUrl))
            {
                await _fileStorageService.DeleteFileAsync(document.DocumentUrl);
            }

            var path = await _fileStorageService.SaveFileAsync(file, "tenant-verification", ct);
            document.DocumentUrl = path;
            document.OriginalFileName = file.FileName;
            document.MimeType = file.ContentType;
            document.FileSize = file.Length;
            document.UploadedAt = DateTime.UtcNow;
            document.ReviewStatus = ApprovalStatus.Pending;
            document.ReviewNotes = null;

            await _documentRepo.UpdateAsync(document);
        }
        else
        {
            var path = await _fileStorageService.SaveFileAsync(file, "tenant-verification", ct);
            document = new Document
            {
                DocumentId = Guid.NewGuid(),
                DocumentType = documentType,
                DocumentUrl = path,
                OriginalFileName = file.FileName,
                MimeType = file.ContentType,
                FileSize = file.Length,
                UploadedById = tenantId,
                UploadedAt = DateTime.UtcNow,
                ReviewStatus = ApprovalStatus.Pending,
                CreatedAt = DateTime.UtcNow
            };

            await _documentRepo.AddAsync(document);

            request.TenantVerificationDocuments.Add(new TenantVerificationDocument
            {
                RequestId = requestId,
                DocumentId = document.DocumentId,
                CreatedAt = DateTime.UtcNow
            });
        }

        return document;
    }
}
