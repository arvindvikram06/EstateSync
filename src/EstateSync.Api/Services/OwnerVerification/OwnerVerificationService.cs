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
using EstateSync.Api.Exceptions;
using EstateSync.Api.Repositories.Interfaces;
using EstateSync.Api.Models;
using EstateSync.Api.Interfaces.Services.OwnerVerification;
using EstateSync.Api.Interfaces.Services.Storage;

namespace EstateSync.Api.Services.OwnerVerification;

public class OwnerVerificationService : IOwnerVerificationService
{
    private readonly IOwnerVerificationRequestRepository _requestRepo;
    private readonly IDocumentRepository _documentRepo;
    private readonly IUserRepository _userRepo;
    private readonly INotificationRepository _notificationRepo;
    private readonly IFileStorageService _fileStorageService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly UserMapper _mapper;
    private readonly ILogger<OwnerVerificationService> _logger;

    public OwnerVerificationService(
        IOwnerVerificationRequestRepository requestRepo,
        IDocumentRepository documentRepo,
        IUserRepository userRepo,
        INotificationRepository notificationRepo,
        IFileStorageService fileStorageService,
        IUnitOfWork unitOfWork,
        UserMapper mapper,
        ILogger<OwnerVerificationService> logger)
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

    public async Task<Guid> CreateDraftVerificationRequestAsync(Guid userId, CancellationToken ct)
    {
        var existingRequests = await _requestRepo.GetByUserIdAsync(userId);
        
        if (existingRequests.Any(r => r.Status == ApprovalStatus.Pending || r.Status == ApprovalStatus.Approved))
        {
            throw new ConflictException("You already have an active or approved owner verification request.");
        }

        var existingDraft = existingRequests.FirstOrDefault(r => r.Status == ApprovalStatus.Draft);
        if (existingDraft != null)
        {
            return existingDraft.RequestId;
        }

        var verificationRequest = new OwnerVerificationRequest
        {
            RequestId = Guid.NewGuid(),
            UserId = userId,
            Status = ApprovalStatus.Draft,
            CreatedAt = DateTime.UtcNow
        };

        await _requestRepo.AddAsync(verificationRequest);
        await _unitOfWork.SaveChangesAsync(ct);

        _logger.LogInformation("Created draft owner verification request {RequestId} for user {UserId}", verificationRequest.RequestId, userId);

        return verificationRequest.RequestId;
    }

    public async Task<DocumentResponse> UploadVerificationDocumentAsync(Guid userId, Guid requestId, IFormFile file, DocumentType docType, CancellationToken ct)
    {
        var request = await _requestRepo.GetWithDetailsByIdAsync(requestId);
        if (request == null)
        {
            throw new NotFoundException("OwnerVerificationRequest", requestId);
        }

        if (request.UserId != userId)
        {
            throw new ForbiddenException("You do not have access to this verification request.");
        }

        if (request.Status != ApprovalStatus.Draft)
        {
            throw new BadRequestException("Documents can only be uploaded to draft requests.");
        }

        var fileUrl = await _fileStorageService.SaveFileAsync(file, $"users/{userId}/documents", ct);

        var existingLink = request.OwnerVerificationDocuments
            .FirstOrDefault(ovd => ovd.Document?.DocumentType == docType);

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

            _logger.LogInformation("Document {DocumentId} of type {DocType} updated for draft request {RequestId} by user {UserId}", doc.DocumentId, docType, requestId, userId);
        }
        else
        {
            doc = new Document
            {
                DocumentId = Guid.NewGuid(),
                DocumentType = docType,
                DocumentUrl = fileUrl,
                OriginalFileName = file.FileName,
                MimeType = file.ContentType,
                FileSize = file.Length,
                UploadedById = userId,
                UploadedAt = DateTime.UtcNow,
                ReviewStatus = ApprovalStatus.Pending,
                CreatedAt = DateTime.UtcNow
            };

            await _documentRepo.AddAsync(doc);

            var link = new OwnerVerificationDocument
            {
                RequestId = requestId,
                DocumentId = doc.DocumentId,
                Document = doc,
                CreatedAt = DateTime.UtcNow
            };

            request.OwnerVerificationDocuments.Add(link);

            _logger.LogInformation("Document {DocumentId} of type {DocType} uploaded and linked to draft request {RequestId} by user {UserId}", doc.DocumentId, docType, requestId, userId);
        }

        await _unitOfWork.SaveChangesAsync(ct);

        return _mapper.MapToDocumentResponse(doc);
    }

    public async Task SubmitVerificationRequestAsync(Guid userId, Guid requestId, CancellationToken ct)
    {
        var request = await _requestRepo.GetWithDetailsByIdAsync(requestId);
        if (request == null)
        {
            throw new NotFoundException("OwnerVerificationRequest", requestId);
        }

        if (request.UserId != userId)
        {
            throw new ForbiddenException("You do not have access to this verification request.");
        }

        if (request.Status != ApprovalStatus.Draft)
        {
            throw new BadRequestException("Only draft requests can be submitted.");
        }

        var linkedDocs = request.OwnerVerificationDocuments
            .Select(ovd => ovd.Document)
            .Where(d => d != null)
            .ToList();

        var requiredTypes = new[] { DocumentType.Aadhaar, DocumentType.PAN, DocumentType.BankProof };
        var missingTypes = requiredTypes.Where(rt => !linkedDocs.Any(d => d!.DocumentType == rt)).ToList();

        if (missingTypes.Any())
        {
            throw new BadRequestException($"Missing required document types: {string.Join(", ", missingTypes)}");
        }

        request.Status = ApprovalStatus.Pending;
        request.CreatedAt = DateTime.UtcNow;

        await _requestRepo.UpdateAsync(request);
        await _unitOfWork.SaveChangesAsync(ct);

        _logger.LogInformation("Owner verification request {RequestId} submitted by user {UserId}", requestId, userId);
    }

    public async Task<IEnumerable<VerificationRequestResponse>> GetUserVerificationRequestsAsync(Guid userId, CancellationToken ct)
    {
        var requests = await _requestRepo.GetByUserIdAsync(userId);
        return requests.Select(_mapper.MapToVerificationRequestResponse).ToList();
    }

    public async Task<PagedResponse<VerificationRequestResponse>> GetAdminVerificationRequestsAsync(string? status, int page, int limit, CancellationToken ct)
    {
        var (items, total) = await _requestRepo.GetPagedRequestsAsync(status, page, limit);
        var mapped = items.Select(_mapper.MapToVerificationRequestResponse).ToList();

        return new PagedResponse<VerificationRequestResponse>
        {
            Success = true,
            Data = mapped,
            Page = page,
            Limit = limit,
            Total = total
        };
    }

    public async Task VerifyRequestAsync(Guid requestId, Guid adminId, string status, string remarks, CancellationToken ct)
    {
        var verificationRequest = await _requestRepo.GetWithDetailsByIdAsync(requestId);
        if (verificationRequest == null)
        {
            throw new NotFoundException("OwnerVerificationRequest", requestId);
        }

        if (verificationRequest.Status != ApprovalStatus.Pending)
        {
            throw new ConflictException("Verification request is not pending review.");
        }

        if (!Enum.TryParse<ApprovalStatus>(status, true, out var parsedStatus) ||
            (parsedStatus != ApprovalStatus.Approved && parsedStatus != ApprovalStatus.Rejected))
        {
            throw new BadRequestException("Invalid status. Must be Approved or Rejected.");
        }

        verificationRequest.Status = parsedStatus;
        verificationRequest.ReviewedById = adminId;
        verificationRequest.ReviewRemarks = remarks;
        verificationRequest.UpdatedAt = DateTime.UtcNow;

        await _requestRepo.UpdateAsync(verificationRequest);

        foreach (var reqDoc in verificationRequest.OwnerVerificationDocuments)
        {
            if (reqDoc.Document != null)
            {
                reqDoc.Document.ReviewStatus = parsedStatus;
                reqDoc.Document.ReviewedAt = DateTime.UtcNow;
                reqDoc.Document.ReviewedById = adminId;
                reqDoc.Document.ReviewNotes = remarks;
                await _documentRepo.UpdateAsync(reqDoc.Document);
            }
        }

        if (parsedStatus == ApprovalStatus.Approved)
        {
            var user = await _userRepo.GetWithRolesByIdAsync(verificationRequest.UserId);
            if (user != null)
            {
                var ownerRoleId = Guid.Parse("22222222-2222-2222-2222-222222222222");
                if (!user.UserRoles.Any(ur => ur.RoleId == ownerRoleId))
                {
                    user.UserRoles.Add(new UserRole
                    {
                        UserId = user.UserId,
                        RoleId = ownerRoleId
                    });
                    await _userRepo.UpdateAsync(user);
                }
            }
        }

        var title = parsedStatus == ApprovalStatus.Approved ? "Verification Approved" : "Verification Rejected";
        var message = parsedStatus == ApprovalStatus.Approved
            ? "Your owner verification request has been approved. You can now list properties."
            : $"Your owner verification request was rejected. Remarks: {remarks}";
        var notifType = parsedStatus == ApprovalStatus.Approved ? NotificationType.VerificationApproved : NotificationType.VerificationRejected;

        var notification = new Notification
        {
            UserId = verificationRequest.UserId,
            Type = notifType,
            Title = title,
            Message = message,
            EntityId = verificationRequest.RequestId.ToString(),
            EntityType = "OwnerVerificationRequest",
            IsRead = false,
            CreatedAt = DateTime.UtcNow
        };
        await _notificationRepo.AddAsync(notification);

        await _unitOfWork.SaveChangesAsync(ct);

        _logger.LogInformation("Verification request {RequestId} was {Status} by admin {AdminId}", requestId, parsedStatus, adminId);
    }
}
