using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using EstateSync.Api.Common;
using EstateSync.Api.DTOs.Kyc;
using EstateSync.Api.DTOs.Users;
using EstateSync.Api.Exceptions;
using EstateSync.Api.Interfaces.Services.Kyc;
using EstateSync.Api.Interfaces.Services.Storage;
using EstateSync.Api.Mappers;
using EstateSync.Api.Models;
using EstateSync.Api.Repositories.Interfaces;

namespace EstateSync.Api.Services.Kyc;

public class KycService : IKycService
{
    private static readonly DocumentType[] AllowedDocumentTypes =
    [
        DocumentType.AadhaarFront,
        DocumentType.AadhaarBack,
        DocumentType.PanCard,
        DocumentType.AddressProof
    ];

    private readonly IKycVerificationRepository _kycRepo;
    private readonly IDocumentRepository _documentRepo;
    private readonly IUserRepository _userRepo;
    private readonly INotificationRepository _notificationRepo;
    private readonly IFileStorageService _fileStorageService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly UserMapper _mapper;
    private readonly ILogger<KycService> _logger;

    public KycService(
        IKycVerificationRepository kycRepo,
        IDocumentRepository documentRepo,
        IUserRepository userRepo,
        INotificationRepository notificationRepo,
        IFileStorageService fileStorageService,
        IUnitOfWork unitOfWork,
        UserMapper mapper,
        ILogger<KycService> logger)
    {
        _kycRepo = kycRepo ?? throw new ArgumentNullException(nameof(kycRepo));
        _documentRepo = documentRepo ?? throw new ArgumentNullException(nameof(documentRepo));
        _userRepo = userRepo ?? throw new ArgumentNullException(nameof(userRepo));
        _notificationRepo = notificationRepo ?? throw new ArgumentNullException(nameof(notificationRepo));
        _fileStorageService = fileStorageService ?? throw new ArgumentNullException(nameof(fileStorageService));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<KycVerificationResponse> CreateDraftAsync(Guid userId, CancellationToken ct)
    {
        var user = await _userRepo.GetByIdAsync(userId)
            ?? throw new NotFoundException("User", userId);

        if (user.IsKycVerified)
            throw new ConflictException("Your KYC is already verified.");

        var existing = await _kycRepo.GetByUserIdAsync(userId);

        if (existing.Any(r => r.Status == ApprovalStatus.Pending || r.Status == ApprovalStatus.Approved))
            throw new ConflictException("You already have an active or approved KYC request.");

        var draft = existing.FirstOrDefault(r => r.Status == ApprovalStatus.Draft);
        if (draft != null)
        {
            var detailed = await _kycRepo.GetWithDetailsByIdAsync(draft.KycVerificationId);
            return _mapper.MapToKycResponse(detailed!);
        }

        var kyc = new KycVerification
        {
            KycVerificationId = Guid.NewGuid(),
            UserId = userId,
            Status = ApprovalStatus.Draft,
            CreatedAt = DateTime.UtcNow
        };

        await _kycRepo.AddAsync(kyc);
        await _unitOfWork.SaveChangesAsync(ct);

        _logger.LogInformation("KYC draft created {KycId} for user {UserId}", kyc.KycVerificationId, userId);

        var result = await _kycRepo.GetWithDetailsByIdAsync(kyc.KycVerificationId);
        return _mapper.MapToKycResponse(result!);
    }

    public async Task<DocumentResponse> UploadDocumentAsync(Guid userId, Guid kycId, IFormFile file, DocumentType documentType, CancellationToken ct)
    {
        if (!AllowedDocumentTypes.Contains(documentType))
            throw new BadRequestException($"Invalid document type. Allowed: {string.Join(", ", AllowedDocumentTypes)}.");

        var kyc = await _kycRepo.GetWithDetailsByIdAsync(kycId)
            ?? throw new NotFoundException("KycVerification", kycId);

        if (kyc.UserId != userId)
            throw new ForbiddenException("You do not have access to this KYC request.");

        if (kyc.Status != ApprovalStatus.Draft)
            throw new ConflictException("Documents can only be uploaded to a draft KYC request.");

        if (file == null || file.Length == 0)
            throw new BadRequestException("No file was uploaded.");

        var isPdf = file.ContentType.Equals("application/pdf", StringComparison.OrdinalIgnoreCase) ||
                    Path.GetExtension(file.FileName).Equals(".pdf", StringComparison.OrdinalIgnoreCase);
        if (!isPdf)
            throw new BadRequestException("Only PDF documents are allowed.");

        var existingLink = kyc.KycDocuments.FirstOrDefault(d => d.Document?.DocumentType == documentType);
        Document doc;

        if (existingLink?.Document != null)
        {
            doc = existingLink.Document;
            if (!string.IsNullOrEmpty(doc.DocumentUrl))
                await _fileStorageService.DeleteFileAsync(doc.DocumentUrl);

            doc.DocumentUrl = await _fileStorageService.SaveFileAsync(file, $"users/{userId}/kyc", ct);
            doc.OriginalFileName = file.FileName;
            doc.MimeType = file.ContentType;
            doc.FileSize = file.Length;
            doc.UploadedAt = DateTime.UtcNow;
            doc.ReviewStatus = ApprovalStatus.Pending;
            doc.UpdatedAt = DateTime.UtcNow;
        }
        else
        {
            var filePath = await _fileStorageService.SaveFileAsync(file, $"users/{userId}/kyc", ct);
            doc = new Document
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

            await _documentRepo.AddAsync(doc);
            kyc.KycDocuments.Add(new KycDocument
            {
                KycVerificationId = kycId,
                DocumentId = doc.DocumentId,
                Document = doc,
                CreatedAt = DateTime.UtcNow
            });
        }

        await _unitOfWork.SaveChangesAsync(ct);
        _logger.LogInformation("KYC document {DocType} uploaded for user {UserId}", documentType, userId);
        return _mapper.MapToDocumentResponse(doc);
    }

    public async Task SubmitAsync(Guid userId, Guid kycId, SubmitKycRequest request, CancellationToken ct)
    {
        var kyc = await _kycRepo.GetWithDetailsByIdAsync(kycId)
            ?? throw new NotFoundException("KycVerification", kycId);

        if (kyc.UserId != userId)
            throw new ForbiddenException("You do not have access to this KYC request.");

        if (kyc.Status != ApprovalStatus.Draft)
            throw new ConflictException("Only draft KYC requests can be submitted.");

        if (await _userRepo.IsAadhaarInUseAsync(request.AadhaarNumber, userId))
            throw new ConflictException("This Aadhaar number is already associated with another account.");

        if (await _userRepo.IsPanInUseAsync(request.PanNumber, userId))
            throw new ConflictException("This PAN number is already associated with another account.");

        ValidateRequiredDocuments(kyc);

        kyc.AadhaarNumber = request.AadhaarNumber;
        kyc.PanNumber = request.PanNumber;
        kyc.Status = ApprovalStatus.Pending;
        kyc.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.SaveChangesAsync(ct);
        _logger.LogInformation("KYC {KycId} submitted by user {UserId}", kycId, userId);
    }

    public async Task<IEnumerable<KycVerificationResponse>> GetByUserIdAsync(Guid userId, CancellationToken ct)
    {
        var records = await _kycRepo.GetByUserIdAsync(userId);
        return records.Select(_mapper.MapToKycResponse);
    }

    public async Task<PagedResponse<KycVerificationResponse>> GetPagedAsync(string? status, int page, int limit, CancellationToken ct)
    {
        var (items, total) = await _kycRepo.GetPagedAsync(status, page, limit);
        return new PagedResponse<KycVerificationResponse>
        {
            Success = true,
            Data = items.Select(_mapper.MapToKycResponse),
            Page = page,
            Limit = limit,
            Total = total
        };
    }

    public async Task ReviewAsync(Guid kycId, Guid reviewerId, string status, string remarks, CancellationToken ct)
    {
        if (!Enum.TryParse<ApprovalStatus>(status, true, out var parsedStatus) ||
            (parsedStatus != ApprovalStatus.Approved && parsedStatus != ApprovalStatus.Rejected))
            throw new BadRequestException("Status must be Approved or Rejected.");

        var kyc = await _kycRepo.GetWithDetailsByIdAsync(kycId)
            ?? throw new NotFoundException("KycVerification", kycId);

        if (kyc.Status != ApprovalStatus.Pending)
            throw new ConflictException("KYC request is not pending review.");

        kyc.Status = parsedStatus;
        kyc.ReviewedById = reviewerId;
        kyc.ReviewRemarks = remarks;
        kyc.ReviewedAt = DateTime.UtcNow;
        kyc.UpdatedAt = DateTime.UtcNow;

        foreach (var kycDoc in kyc.KycDocuments.Where(d => d.Document != null))
        {
            kycDoc.Document!.ReviewStatus = parsedStatus;
            kycDoc.Document.ReviewedAt = DateTime.UtcNow;
            kycDoc.Document.ReviewedById = reviewerId;
            kycDoc.Document.ReviewNotes = remarks;
        }

        if (parsedStatus == ApprovalStatus.Approved)
        {
            var user = await _userRepo.GetByIdAsync(kyc.UserId);
            if (user != null)
            {
                user.IsKycVerified = true;
                user.AadhaarNumber = kyc.AadhaarNumber;
                user.PanNumber = kyc.PanNumber;
                user.UpdatedAt = DateTime.UtcNow;
            }
        }

        await _notificationRepo.AddAsync(new Notification
        {
            NotificationId = Guid.NewGuid(),
            UserId = kyc.UserId,
            Type = parsedStatus == ApprovalStatus.Approved ? NotificationType.VerificationApproved : NotificationType.VerificationRejected,
            Title = parsedStatus == ApprovalStatus.Approved ? "KYC Verified" : "KYC Rejected",
            Message = parsedStatus == ApprovalStatus.Approved
                ? "Your KYC verification has been approved. You can now access all platform features."
                : $"Your KYC was rejected. Remarks: {remarks}",
            EntityId = kyc.KycVerificationId.ToString(),
            EntityType = "KycVerification",
            IsRead = false,
            CreatedAt = DateTime.UtcNow
        });

        await _unitOfWork.SaveChangesAsync(ct);
        _logger.LogInformation("KYC {KycId} {Status} by reviewer {ReviewerId}", kycId, parsedStatus, reviewerId);
    }

    private static void ValidateRequiredDocuments(KycVerification kyc)
    {
        var uploadedTypes = kyc.KycDocuments
            .Where(d => d.Document != null)
            .Select(d => d.Document!.DocumentType)
            .ToHashSet();

        var hasAadhaar = uploadedTypes.Contains(DocumentType.AadhaarFront) || uploadedTypes.Contains(DocumentType.AadhaarBack);
        var hasPan = uploadedTypes.Contains(DocumentType.PanCard);
        var hasAddress = uploadedTypes.Contains(DocumentType.AddressProof);

        var missing = new List<string>();
        if (!hasAadhaar) missing.Add("Aadhaar (front or back)");
        if (!hasPan) missing.Add("PAN card");
        if (!hasAddress) missing.Add("Address proof");

        if (missing.Any())
            throw new BadRequestException($"Missing required documents: {string.Join(", ", missing)}.");
    }
}
