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
using EstateSync.Api.DTOs.OwnerVerification;
using EstateSync.Api.Exceptions;
using EstateSync.Api.Repositories.Interfaces;
using EstateSync.Api.Models;
using EstateSync.Api.Interfaces.Services.OwnerVerification;
using EstateSync.Api.Interfaces.Services.Storage;
using EstateSync.Api.Interfaces.Services.Auth;

namespace EstateSync.Api.Services.OwnerVerification;

public class OwnerVerificationService : IOwnerVerificationService
{
    private readonly IOwnerVerificationRequestRepository _requestRepo;
    private readonly IDocumentRepository _documentRepo;
    private readonly IUserRepository _userRepo;
    private readonly INotificationRepository _notificationRepo;
    private readonly IFileStorageService _fileStorageService;
    private readonly IRoleRepository _roleRepo;
    private readonly IUnitOfWork _unitOfWork;
    private readonly UserMapper _mapper;
    private readonly ILogger<OwnerVerificationService> _logger;
    private readonly ICurrentUserService _currentUserService;

    public OwnerVerificationService(
        IOwnerVerificationRequestRepository requestRepo,
        IDocumentRepository documentRepo,
        IUserRepository userRepo,
        INotificationRepository notificationRepo,
        IFileStorageService fileStorageService,
        IRoleRepository roleRepo,
        IUnitOfWork unitOfWork,
        UserMapper mapper,
        ILogger<OwnerVerificationService> logger,
        ICurrentUserService currentUserService)
    {
        _requestRepo = requestRepo ?? throw new ArgumentNullException(nameof(requestRepo));
        _documentRepo = documentRepo ?? throw new ArgumentNullException(nameof(documentRepo));
        _userRepo = userRepo ?? throw new ArgumentNullException(nameof(userRepo));
        _notificationRepo = notificationRepo ?? throw new ArgumentNullException(nameof(notificationRepo));
        _fileStorageService = fileStorageService ?? throw new ArgumentNullException(nameof(fileStorageService));
        _roleRepo = roleRepo ?? throw new ArgumentNullException(nameof(roleRepo));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _currentUserService = currentUserService ?? throw new ArgumentNullException(nameof(currentUserService));
    }

    public async Task<OwnerVerificationResponse> CreateDraftVerificationRequestAsync(Guid userId, CancellationToken ct)
    {
        var user = await _currentUserService.GetUserOrThrowAsync(userId, ct);

        if (!user.IsIdentityVerified)
        {
            throw new ForbiddenException("You must verify your identity before submitting an owner verification request.");
        }

        var existingRequests = await _requestRepo.GetByUserIdAsync(userId);
        
        if (existingRequests.Any(r => r.Status == ApprovalStatus.Pending || r.Status == ApprovalStatus.Approved))
        {
            throw new ConflictException("You already have an active or approved owner verification request.");
        }

        var existingDraft = existingRequests.FirstOrDefault(r => r.Status == ApprovalStatus.Draft);
        if (existingDraft != null)
        {
            var detailedDraft = await _requestRepo.GetWithDetailsByIdAsync(existingDraft.RequestId);
            return _mapper.MapToVerificationRequestResponse(detailedDraft!);
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

        var detailedRequest = await _requestRepo.GetWithDetailsByIdAsync(verificationRequest.RequestId);
        return _mapper.MapToVerificationRequestResponse(detailedRequest!);
    }

    public async Task<DocumentResponse> UploadVerificationDocumentAsync(Guid userId, Guid requestId, IFormFile file, DocumentType docType, CancellationToken ct)
    {
        if (docType != DocumentType.Aadhaar && docType != DocumentType.PAN && docType != DocumentType.BankProof)
        {
            throw new BadRequestException("Only Aadhaar, PAN, and BankProof documents are allowed for owner verification.");
        }

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
        var existingLink = request.OwnerVerificationDocuments
            .FirstOrDefault(ovd => ovd.Document?.DocumentType == docType);

        Document doc;
        if (existingLink != null && existingLink.Document != null)
        {
            doc = existingLink.Document;
            if (!string.IsNullOrEmpty(doc.DocumentUrl))
            {
                await _fileStorageService.DeleteFileAsync(doc.DocumentUrl);
            }

            var fileUrl = await _fileStorageService.SaveFileAsync(file, $"users/{userId}/documents", ct);
            doc.DocumentUrl = fileUrl;
            doc.OriginalFileName = file.FileName;
            doc.MimeType = file.ContentType;
            doc.FileSize = file.Length;
            doc.UploadedAt = DateTime.UtcNow;
            doc.ReviewStatus = ApprovalStatus.Pending;
            doc.UpdatedAt = DateTime.UtcNow;

            await _documentRepo.UpdateAsync(doc);
        }
        else
        {
            var fileUrl = await _fileStorageService.SaveFileAsync(file, $"users/{userId}/documents", ct);
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
        }

        _logger.LogInformation("Document {DocumentId} of type {DocType} uploaded/updated and linked to draft request {RequestId} by user {UserId}", doc.DocumentId, docType, requestId, userId);

        await _unitOfWork.SaveChangesAsync(ct);
        return _mapper.MapToDocumentResponse(doc);
    }

    public async Task SubmitVerificationRequestAsync(Guid userId, Guid requestId, SubmitOwnerVerificationRequest requestDto, CancellationToken ct)
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

        ValidateAndPopulateSubmission(request, requestDto);

        await _requestRepo.UpdateAsync(request);
        await _unitOfWork.SaveChangesAsync(ct);

        _logger.LogInformation("Owner verification request {RequestId} submitted by user {UserId}", requestId, userId);
    }

    public async Task<IEnumerable<OwnerVerificationResponse>> GetOwnerVerificationRequestsByUserIdAsync(Guid userId, CancellationToken ct)
    {
        var requests = await _requestRepo.GetByUserIdAsync(userId);
        return requests.Select(_mapper.MapToVerificationRequestResponse).ToList();
    }

    public async Task<PagedResponse<OwnerVerificationResponse>> GetPagedOwnerVerificationRequestsAsync(string? status, int page, int limit, CancellationToken ct)
    {
        var (items, total) = await _requestRepo.GetPagedRequestsAsync(status, page, limit);
        var mapped = items.Select(_mapper.MapToVerificationRequestResponse).ToList();

        return new PagedResponse<OwnerVerificationResponse>
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

        await UpdateDocumentReviewStatusAsync(verificationRequest, parsedStatus, adminId, remarks);

        if (parsedStatus == ApprovalStatus.Approved)
        {
            await AssignOwnerRoleAndProfileAsync(verificationRequest);
        }

        await CreateAndSendVerificationNotificationAsync(verificationRequest, parsedStatus, remarks);
        await _unitOfWork.SaveChangesAsync(ct);

        _logger.LogInformation("Verification request {RequestId} was {Status} by admin {AdminId}", requestId, parsedStatus, adminId);
    }



    private void ValidateAndPopulateSubmission(OwnerVerificationRequest request, SubmitOwnerVerificationRequest requestDto)
    {
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

        request.BankAccountNumber = requestDto.BankAccountNumber;
        request.BankIFSCCode = requestDto.BankIFSCCode;
        request.BankAccountHolderName = requestDto.BankAccountHolderName;
        request.BankName = requestDto.BankName;
        request.Status = ApprovalStatus.Pending;
        request.CreatedAt = DateTime.UtcNow;
    }

    private async Task UpdateDocumentReviewStatusAsync(OwnerVerificationRequest request, ApprovalStatus status, Guid adminId, string remarks)
    {
        foreach (var reqDoc in request.OwnerVerificationDocuments)
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

    private async Task AssignOwnerRoleAndProfileAsync(OwnerVerificationRequest request)
    {
        var user = await _userRepo.GetByIdWithRolesAsync(request.UserId);
        if (user != null)
        {
            var ownerRole = await _roleRepo.GetByNameAsync("Owner");
            if (ownerRole == null)
            {
                throw new NotFoundException("Role", "Owner");
            }

            if (!user.UserRoles.Any(ur => ur.RoleId == ownerRole.RoleId))
            {
                user.UserRoles.Add(new UserRole
                {
                    UserId = user.UserId,
                    RoleId = ownerRole.RoleId
                });
            }

            if (user.OwnerProfile == null)
            {
                user.OwnerProfile = new OwnerProfile
                {
                    OwnerProfileId = Guid.NewGuid(),
                    UserId = user.UserId,
                    BankAccountNumber = request.BankAccountNumber ?? string.Empty,
                    BankIFSCCode = request.BankIFSCCode ?? string.Empty,
                    BankAccountHolderName = request.BankAccountHolderName ?? string.Empty,
                    BankName = request.BankName ?? string.Empty,
                    CreatedAt = DateTime.UtcNow
                };
            }
            else
            {
                user.OwnerProfile.BankAccountNumber = request.BankAccountNumber ?? string.Empty;
                user.OwnerProfile.BankIFSCCode = request.BankIFSCCode ?? string.Empty;
                user.OwnerProfile.BankAccountHolderName = request.BankAccountHolderName ?? string.Empty;
                user.OwnerProfile.BankName = request.BankName ?? string.Empty;
                user.OwnerProfile.UpdatedAt = DateTime.UtcNow;
            }

            await _userRepo.UpdateAsync(user);
        }
    }

    private async Task CreateAndSendVerificationNotificationAsync(OwnerVerificationRequest request, ApprovalStatus status, string remarks)
    {
        var title = status == ApprovalStatus.Approved ? "Verification Approved" : "Verification Rejected";
        var message = status == ApprovalStatus.Approved
            ? "Your owner verification request has been approved. You can now list properties."
            : $"Your owner verification request was rejected. Remarks: {remarks}";
        var notifType = status == ApprovalStatus.Approved ? NotificationType.VerificationApproved : NotificationType.VerificationRejected;

        var notification = new Notification
        {
            NotificationId = Guid.NewGuid(),
            UserId = request.UserId,
            Type = notifType,
            Title = title,
            Message = message,
            EntityId = request.RequestId.ToString(),
            EntityType = "OwnerVerificationRequest",
            IsRead = false,
            CreatedAt = DateTime.UtcNow
        };
        await _notificationRepo.AddAsync(notification);
    }
}
