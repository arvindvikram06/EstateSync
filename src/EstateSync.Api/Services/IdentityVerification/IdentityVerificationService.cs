using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EstateSync.Api.Mappers;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using EstateSync.Api.Common;
using EstateSync.Api.DTOs.Users;
using EstateSync.Api.DTOs.IdentityVerification;
using EstateSync.Api.Exceptions;
using EstateSync.Api.Repositories.Interfaces;
using EstateSync.Api.Models;
using EstateSync.Api.Interfaces.Services.IdentityVerification;
using EstateSync.Api.Interfaces.Services.Storage;
using Microsoft.VisualBasic;

namespace EstateSync.Api.Services.IdentityVerification;

public class IdentityVerificationService : IIdentityVerificationService
{
    private readonly IIdentityVerificationRequestRepository _requestRepo;
    private readonly IDocumentRepository _documentRepo;
    private readonly IUserRepository _userRepo;
    private readonly INotificationRepository _notificationRepo;
    private readonly IFileStorageService _fileStorageService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly UserMapper _mapper;
    private readonly ILogger<IdentityVerificationService> _logger;

    public IdentityVerificationService(
        IIdentityVerificationRequestRepository requestRepo,
        IDocumentRepository documentRepo,
        IUserRepository userRepo,
        INotificationRepository notificationRepo,
        IFileStorageService fileStorageService,
        IUnitOfWork unitOfWork,
        UserMapper mapper,
        ILogger<IdentityVerificationService> logger)
    {
        _requestRepo = requestRepo ?? throw new ArgumentNullException(nameof(requestRepo));
        _documentRepo = documentRepo ?? throw new ArgumentNullException(nameof(documentRepo));
        _userRepo = userRepo ?? throw new ArgumentNullException(nameof(userRepo));
        _notificationRepo = notificationRepo ?? throw new ArgumentNullException(nameof(notificationRepo));
        _fileStorageService = fileStorageService ?? throw new ArgumentNullException(nameof(fileStorageService));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }


    // Creates a new draft identity verification request for the user if no active or approved requests exist. If a draft already exists, returns the existing draft's ID.
    public async Task<IdentityVerificationResponse> CreateDraftIdentityRequestAsync(Guid userId, CancellationToken ct)
    {
        var existingRequests = await _requestRepo.GetByUserIdAsync(userId);
        
        if (existingRequests.Any(r => r.Status == ApprovalStatus.Pending || r.Status == ApprovalStatus.Approved))
        {
            throw new ConflictException("You already have an active or approved identity verification request.");
        }
        
        // return if draft already exists
        var existingDraft = existingRequests.FirstOrDefault(r => r.Status == ApprovalStatus.Draft);
        if (existingDraft != null)
        {
            var detailedDraft = await _requestRepo.GetWithDetailsByIdAsync(existingDraft.RequestId);
            return _mapper.MapToIdentityResponse(detailedDraft!);
        }

        // create fresh draft
        var verificationRequest = new IdentityVerificationRequest
        {
            RequestId = Guid.NewGuid(),
            UserId = userId,
            Status = ApprovalStatus.Draft,
            CreatedAt = DateTime.UtcNow
        };

        await _requestRepo.AddAsync(verificationRequest);
        await _unitOfWork.SaveChangesAsync(ct);

        _logger.LogInformation("Created draft identity verification request {RequestId} for user {UserId}", verificationRequest.RequestId, userId);

        var detailedRequest = await _requestRepo.GetWithDetailsByIdAsync(verificationRequest.RequestId);
        return _mapper.MapToIdentityResponse(detailedRequest!);
    }


    // Handles both initial upload and replacement of documents. If a document of the same type already exists for the request, it will be replaced with the new one.
    public async Task<DocumentResponse> UploadIdentityDocumentAsync(Guid userId, Guid requestId, IFormFile file, DocumentType documentType, CancellationToken ct)
    {
        if (documentType != DocumentType.Aadhaar && documentType != DocumentType.PAN)
        {
            throw new BadRequestException("Only Aadhaar and PAN documents are allowed for identity verification.");
        }

        var verificationRequest = await _requestRepo.GetWithDetailsByIdAsync(requestId);
        if (verificationRequest == null || verificationRequest.UserId != userId)
        {
            throw new NotFoundException("IdentityVerificationRequest", requestId);
        }

        if (verificationRequest.Status != ApprovalStatus.Draft)
        {
            throw new ConflictException("You can only upload documents to a draft request.");
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

        // Find and replace/update any existing documents of this type linked to the request
        var existingLink = verificationRequest.IdentityVerificationDocuments
            .FirstOrDefault(ivd => ivd.Document?.DocumentType == documentType);

        Document document;
        if (existingLink != null && existingLink.Document != null)
        {
            document = existingLink.Document;
            if (!string.IsNullOrEmpty(document.DocumentUrl))
            {
                await _fileStorageService.DeleteFileAsync(document.DocumentUrl);
            }

            var filePath = await _fileStorageService.SaveFileAsync(file, "identity-verification", ct);
            document.DocumentUrl = filePath;
            document.OriginalFileName = file.FileName;
            document.MimeType = file.ContentType;
            document.FileSize = file.Length;
            document.UploadedAt = DateTime.UtcNow;
            document.ReviewStatus = ApprovalStatus.Pending;
            document.UpdatedAt = DateTime.UtcNow;

            await _documentRepo.UpdateAsync(document);
        }
        else
        {
            var filePath = await _fileStorageService.SaveFileAsync(file, "identity-verification", ct);
            document = new Document
            {
                DocumentId = Guid.NewGuid(),
                DocumentType = documentType,
                DocumentUrl = filePath,
                OriginalFileName = file.FileName,
                MimeType = file.ContentType,
                FileSize = file.Length,
                UploadedById = userId,
                UploadedAt = DateTime.UtcNow,
                ReviewStatus = ApprovalStatus.Pending,
                CreatedAt = DateTime.UtcNow
            };

            await _documentRepo.AddAsync(document);

            verificationRequest.IdentityVerificationDocuments.Add(new IdentityVerificationDocument
            {
                // IdentityVerificationDocumentId = Guid.NewGuid(),
                RequestId = requestId,
                DocumentId = document.DocumentId,
                Document = document,
                CreatedAt = DateTime.UtcNow
            });
        }

        _logger.LogInformation("Uploaded/Updated document type {DocumentType} for identity request {RequestId} of user {UserId}", documentType, requestId, userId);

        await _unitOfWork.SaveChangesAsync(ct);
        return _mapper.MapToDocumentResponse(document);
    }

    public async Task SubmitIdentityRequestAsync(Guid userId, Guid requestId, SubmitIdentityVerificationRequest request, CancellationToken ct)
    {
        var verificationRequest = await _requestRepo.GetWithDetailsByIdAsync(requestId);
        if (verificationRequest == null || verificationRequest.UserId != userId)
        {
            throw new NotFoundException("IdentityVerificationRequest", requestId);
        }

        if (verificationRequest.Status != ApprovalStatus.Draft)
        {
            throw new ConflictException("Request is not in a draft state.");
        }

        ValidateAndPopulateIdentitySubmission(verificationRequest, request);

        await _requestRepo.UpdateAsync(verificationRequest);
        await _unitOfWork.SaveChangesAsync(ct);

        _logger.LogInformation("Submitted identity verification request {RequestId} for user {UserId}", requestId, userId);
    }

    public async Task<IEnumerable<IdentityVerificationResponse>> GetIdentityRequestsByUserIdAsync(Guid userId, CancellationToken ct)
    {
        var requests = await _requestRepo.GetByUserIdAsync(userId);
        return requests.Select(r => _mapper.MapToIdentityResponse(r));
    }

    public async Task<PagedResponse<IdentityVerificationResponse>> GetPagedIdentityRequestsAsync(string? status, int page, int limit, CancellationToken ct)
    {
        var (items, total) = await _requestRepo.GetPagedRequestsAsync(status, page, limit);
        var mapped = items.Select(r => _mapper.MapToIdentityResponse(r)).ToList();

        return new PagedResponse<IdentityVerificationResponse>
        {
            Success = true,
            Data = mapped,
            Page = page,
            Limit = limit,
            Total = total
        };
    }

    public async Task VerifyIdentityRequestAsync(Guid id, Guid adminId, string status, string remarks, CancellationToken ct)
    {
        if (!Enum.TryParse<ApprovalStatus>(status, true, out var parsedStatus) ||
            (parsedStatus != ApprovalStatus.Approved && parsedStatus != ApprovalStatus.Rejected))
        {
            throw new BadRequestException("Status must be either 'Approved' or 'Rejected'.");
        }

        var verificationRequest = await _requestRepo.GetWithDetailsByIdAsync(id);
        if (verificationRequest == null)
        {
            throw new NotFoundException("IdentityVerificationRequest", id);
        }

        if (verificationRequest.Status != ApprovalStatus.Pending)
        {
            throw new ConflictException("Identity verification request is not pending.");
        }

        verificationRequest.Status = parsedStatus;
        verificationRequest.ReviewedById = adminId;
        verificationRequest.ReviewRemarks = remarks;
        verificationRequest.UpdatedAt = DateTime.UtcNow;

        await _requestRepo.UpdateAsync(verificationRequest);

        await UpdateIdentityDocumentsReviewStatusAsync(verificationRequest, parsedStatus, adminId, remarks);

        if (parsedStatus == ApprovalStatus.Approved)
        {
            await ApplyUserIdentityVerificationAsync(verificationRequest);
        }

        await CreateAndSendIdentityNotificationAsync(verificationRequest, parsedStatus, remarks);
        await _unitOfWork.SaveChangesAsync(ct);

        _logger.LogInformation("Identity verification request {RequestId} was {Status} by admin {AdminId}", id, parsedStatus, adminId);
    }



    private void ValidateAndPopulateIdentitySubmission(IdentityVerificationRequest verificationRequest, SubmitIdentityVerificationRequest request)
    {
        var linkedDocs = verificationRequest.IdentityVerificationDocuments
            .Select(ivd => ivd.Document)
            .Where(d => d != null)
            .ToList();

        var requiredTypes = new[] { DocumentType.Aadhaar, DocumentType.PAN };
        var missingTypes = requiredTypes.Where(rt => !linkedDocs.Any(d => d!.DocumentType == rt)).ToList();

        if (missingTypes.Any())
        {
            throw new BadRequestException($"Missing required document types: {string.Join(", ", missingTypes)}");
        }

        verificationRequest.AadhaarNumber = request.AadhaarNumber;
        verificationRequest.PanNumber = request.PanNumber;
        verificationRequest.Status = ApprovalStatus.Pending;
        verificationRequest.UpdatedAt = DateTime.UtcNow;
    }

    private async Task UpdateIdentityDocumentsReviewStatusAsync(IdentityVerificationRequest request, ApprovalStatus status, Guid adminId, string remarks)
    {
        foreach (var reqDoc in request.IdentityVerificationDocuments)
        {
            if (reqDoc.Document != null)
            {
                reqDoc.Document.ReviewStatus = status;
                reqDoc.Document.ReviewedAt = DateTime.UtcNow;
                reqDoc.Document.ReviewedById = adminId;
                reqDoc.Document.ReviewNotes = remarks;
                await _documentRepo.UpdateAsync(reqDoc.Document);
            }
        }
    }

    private async Task ApplyUserIdentityVerificationAsync(IdentityVerificationRequest request)
    {
        var user = await _userRepo.GetByIdWithRolesAsync(request.UserId);
        if (user != null)
        {
            user.AadhaarNumber = request.AadhaarNumber;
            user.PanNumber = request.PanNumber;
            user.IsIdentityVerified = true;
            user.UpdatedAt = DateTime.UtcNow;
            await _userRepo.UpdateAsync(user);
        }
    }

    private async Task CreateAndSendIdentityNotificationAsync(IdentityVerificationRequest request, ApprovalStatus status, string remarks)
    {
        var title = status == ApprovalStatus.Approved ? "Identity Verification Approved" : "Identity Verification Rejected";
        var message = status == ApprovalStatus.Approved
            ? "Your identity verification request has been approved."
            : $"Your identity verification request was rejected. Remarks: {remarks}";
        var notifType = status == ApprovalStatus.Approved ? NotificationType.VerificationApproved : NotificationType.VerificationRejected;

        var notification = new Notification
        {
            NotificationId = Guid.NewGuid(),
            UserId = request.UserId,
            Type = notifType,
            Title = title,
            Message = message,
            EntityId = request.RequestId.ToString(),
            EntityType = "IdentityVerificationRequest",
            IsRead = false,
            CreatedAt = DateTime.UtcNow
        };
        await _notificationRepo.AddAsync(notification);
    }
}
