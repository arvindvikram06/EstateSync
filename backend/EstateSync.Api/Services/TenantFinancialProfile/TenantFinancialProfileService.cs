using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using EstateSync.Api.Common;
using EstateSync.Api.DTOs.TenantFinancialProfile;
using EstateSync.Api.DTOs.Users;
using EstateSync.Api.Exceptions;
using EstateSync.Api.Interfaces.Services.Notifications;
using EstateSync.Api.Interfaces.Services.Storage;
using EstateSync.Api.Interfaces.Services.TenantFinancialProfile;
using EstateSync.Api.Mappers;
using EstateSync.Api.Models;
using EstateSync.Api.Repositories.Interfaces;

namespace EstateSync.Api.Services.TenantFinancialProfile;

public class TenantFinancialProfileService : ITenantFinancialProfileService
{
    private static readonly DocumentType[] AllowedDocumentTypes =
    [
        DocumentType.SalarySlip,
        DocumentType.BankStatement,
        DocumentType.EmploymentLetter,
        DocumentType.IncomeTaxReturn
    ];

    private readonly ITenantFinancialProfileRepository _profileRepo;
    private readonly IDocumentRepository _documentRepo;
    private readonly IUserRepository _userRepo;
    private readonly INotificationSender _notificationSender;
    private readonly IFileStorageService _fileStorageService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly UserMapper _mapper;
    private readonly ILogger<TenantFinancialProfileService> _logger;

    public TenantFinancialProfileService(
        ITenantFinancialProfileRepository profileRepo,
        IDocumentRepository documentRepo,
        IUserRepository userRepo,
        INotificationSender notificationSender,
        IFileStorageService fileStorageService,
        IUnitOfWork unitOfWork,
        UserMapper mapper,
        ILogger<TenantFinancialProfileService> logger)
    {
        _profileRepo = profileRepo ?? throw new ArgumentNullException(nameof(profileRepo));
        _documentRepo = documentRepo ?? throw new ArgumentNullException(nameof(documentRepo));
        _userRepo = userRepo ?? throw new ArgumentNullException(nameof(userRepo));
        _notificationSender = notificationSender ?? throw new ArgumentNullException(nameof(notificationSender));
        _fileStorageService = fileStorageService ?? throw new ArgumentNullException(nameof(fileStorageService));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<TenantFinancialProfileResponse> CreateDraftAsync(Guid tenantId, CancellationToken ct)
    {
        var tenant = await _userRepo.GetByIdAsync(tenantId)
            ?? throw new NotFoundException("User", tenantId);

        if (!tenant.IsKycVerified)
            throw new ForbiddenException("You must complete KYC verification before submitting a financial profile.");

        var existing = await _profileRepo.GetByTenantIdAsync(tenantId);

        if (existing.Any(p => p.Status == ApprovalStatus.Pending || p.Status == ApprovalStatus.Approved))
            throw new ConflictException("You already have an active or approved financial profile.");

        var draft = existing.FirstOrDefault(p => p.Status == ApprovalStatus.Draft);
        if (draft != null)
        {
            var detailed = await _profileRepo.GetWithDetailsByIdAsync(draft.ProfileId);
            return _mapper.MapToFinancialProfileResponse(detailed!);
        }

        var profile = new Models.TenantFinancialProfile
        {
            ProfileId = Guid.NewGuid(),
            TenantId = tenantId,
            Status = ApprovalStatus.Draft,
            CreatedAt = DateTime.UtcNow
        };

        await _profileRepo.AddAsync(profile);
        await _unitOfWork.SaveChangesAsync(ct);

        _logger.LogInformation("Financial profile draft created {ProfileId} for tenant {TenantId}", profile.ProfileId, tenantId);

        var result = await _profileRepo.GetWithDetailsByIdAsync(profile.ProfileId);
        return _mapper.MapToFinancialProfileResponse(result!);
    }

    public async Task<DocumentResponse> UploadDocumentAsync(Guid tenantId, Guid profileId, IFormFile file, DocumentType documentType, CancellationToken ct)
    {
        if (!AllowedDocumentTypes.Contains(documentType))
            throw new BadRequestException($"Invalid document type. Allowed: {string.Join(", ", AllowedDocumentTypes)}.");

        var profile = await _profileRepo.GetWithDetailsByIdAsync(profileId)
            ?? throw new NotFoundException("TenantFinancialProfile", profileId);

        if (profile.TenantId != tenantId)
            throw new ForbiddenException("You do not have access to this financial profile.");

        if (profile.Status != ApprovalStatus.Draft)
            throw new ConflictException("Documents can only be uploaded to a draft profile.");

        if (file == null || file.Length == 0)
            throw new BadRequestException("No file was uploaded.");

        var isPdf = file.ContentType.Equals("application/pdf", StringComparison.OrdinalIgnoreCase) ||
                    Path.GetExtension(file.FileName).Equals(".pdf", StringComparison.OrdinalIgnoreCase);
        if (!isPdf)
            throw new BadRequestException("Only PDF documents are allowed.");

        var existingLink = profile.TenantFinancialDocuments.FirstOrDefault(d => d.Document?.DocumentType == documentType);
        Document doc;

        if (existingLink?.Document != null)
        {
            doc = existingLink.Document;
            if (!string.IsNullOrEmpty(doc.DocumentUrl))
                await _fileStorageService.DeleteFileAsync(doc.DocumentUrl);

            doc.DocumentUrl = await _fileStorageService.SaveFileAsync(file, $"users/{tenantId}/financial-profile", ct);
            doc.OriginalFileName = file.FileName;
            doc.MimeType = file.ContentType;
            doc.FileSize = file.Length;
            doc.UploadedAt = DateTime.UtcNow;
            doc.ReviewStatus = ApprovalStatus.Pending;
            doc.UpdatedAt = DateTime.UtcNow;
        }
        else
        {
            var filePath = await _fileStorageService.SaveFileAsync(file, $"users/{tenantId}/financial-profile", ct);
            doc = new Document
            {
                DocumentId = Guid.NewGuid(),
                DocumentType = documentType,
                DocumentUrl = filePath,
                OriginalFileName = file.FileName,
                MimeType = file.ContentType,
                FileSize = file.Length,
                UploadedById = tenantId,
                UploadedAt = DateTime.UtcNow,
                ReviewStatus = ApprovalStatus.Pending,
                CreatedAt = DateTime.UtcNow
            };

            await _documentRepo.AddAsync(doc);
            profile.TenantFinancialDocuments.Add(new TenantFinancialDocument
            {
                ProfileId = profileId,
                DocumentId = doc.DocumentId,
                Document = doc,
                CreatedAt = DateTime.UtcNow
            });
        }

        await _unitOfWork.SaveChangesAsync(ct);
        _logger.LogInformation("Financial profile document {DocType} uploaded for tenant {TenantId}", documentType, tenantId);
        return _mapper.MapToDocumentResponse(doc);
    }

    public async Task SubmitAsync(Guid tenantId, Guid profileId, SubmitTenantFinancialProfileRequest request, CancellationToken ct)
    {
        var profile = await _profileRepo.GetWithDetailsByIdAsync(profileId)
            ?? throw new NotFoundException("TenantFinancialProfile", profileId);

        if (profile.TenantId != tenantId)
            throw new ForbiddenException("You do not have access to this financial profile.");

        if (profile.Status != ApprovalStatus.Draft)
            throw new ConflictException("Only draft profiles can be submitted.");

        ValidateRequiredDocuments(profile, request.EmploymentType);

        profile.EmploymentType = request.EmploymentType;
        profile.EmployerName = request.EmployerName;
        profile.DeclaredMonthlyIncome = request.DeclaredMonthlyIncome;
        profile.Status = ApprovalStatus.Pending;
        profile.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.SaveChangesAsync(ct);
        _logger.LogInformation("Financial profile {ProfileId} submitted by tenant {TenantId}", profileId, tenantId);
    }

    public async Task<IEnumerable<TenantFinancialProfileResponse>> GetByTenantIdAsync(Guid tenantId, CancellationToken ct)
    {
        var profiles = await _profileRepo.GetByTenantIdAsync(tenantId);
        return profiles.Select(_mapper.MapToFinancialProfileResponse);
    }

    public async Task<PagedResponse<TenantFinancialProfileResponse>> GetPagedAsync(string? status, int page, int limit, CancellationToken ct)
    {
        var (items, total) = await _profileRepo.GetPagedAsync(status, page, limit);
        return new PagedResponse<TenantFinancialProfileResponse>
        {
            Success = true,
            Data = items.Select(_mapper.MapToFinancialProfileResponse),
            Page = page,
            Limit = limit,
            Total = total
        };
    }

    public async Task ReviewAsync(Guid profileId, Guid reviewerId, string status, string remarks, CancellationToken ct)
    {
        if (!Enum.TryParse<ApprovalStatus>(status, true, out var parsedStatus) ||
            (parsedStatus != ApprovalStatus.Approved && parsedStatus != ApprovalStatus.Rejected))
            throw new BadRequestException("Status must be Approved or Rejected.");

        var profile = await _profileRepo.GetWithDetailsByIdAsync(profileId)
            ?? throw new NotFoundException("TenantFinancialProfile", profileId);

        if (profile.Status != ApprovalStatus.Pending)
            throw new ConflictException("Financial profile is not pending review.");

        profile.Status = parsedStatus;
        profile.ReviewedById = reviewerId;
        profile.ReviewRemarks = remarks;
        profile.ReviewedAt = DateTime.UtcNow;
        profile.UpdatedAt = DateTime.UtcNow;

        foreach (var doc in profile.TenantFinancialDocuments.Where(d => d.Document != null))
        {
            doc.Document!.ReviewStatus = parsedStatus;
            doc.Document.ReviewedAt = DateTime.UtcNow;
            doc.Document.ReviewedById = reviewerId;
            doc.Document.ReviewNotes = remarks;
        }

        if (parsedStatus == ApprovalStatus.Approved)
        {
            var tenant = await _userRepo.GetByIdAsync(profile.TenantId);
            if (tenant != null)
            {
                tenant.IsFinanciallyVerified = true;
                tenant.VerifiedMonthlyIncome = profile.DeclaredMonthlyIncome;
                tenant.UpdatedAt = DateTime.UtcNow;
            }
        }

        await _notificationSender.SendAsync(new Notification
        {
            NotificationId = Guid.NewGuid(),
            UserId = profile.TenantId,
            Type = parsedStatus == ApprovalStatus.Approved ? NotificationType.VerificationApproved : NotificationType.VerificationRejected,
            Title = parsedStatus == ApprovalStatus.Approved ? "Financial Profile Verified" : "Financial Profile Rejected",
            Message = parsedStatus == ApprovalStatus.Approved
                ? "Your financial profile has been verified. You will now appear as an income-verified tenant to owners."
                : $"Your financial profile was rejected. Remarks: {remarks}",
            EntityId = profile.ProfileId.ToString(),
            EntityType = "TenantFinancialProfile",
            IsRead = false,
            CreatedAt = DateTime.UtcNow
        });

        await _unitOfWork.SaveChangesAsync(ct);
        _logger.LogInformation("Financial profile {ProfileId} {Status} by reviewer {ReviewerId}", profileId, parsedStatus, reviewerId);
    }

    private static void ValidateRequiredDocuments(Models.TenantFinancialProfile profile, EmploymentType employmentType)
    {
        var uploadedTypes = profile.TenantFinancialDocuments
            .Where(d => d.Document != null)
            .Select(d => d.Document!.DocumentType)
            .ToHashSet();

        var missing = new List<string>();

        if (employmentType == EmploymentType.Salaried)
        {
            if (!uploadedTypes.Contains(DocumentType.SalarySlip)) missing.Add("Salary slip");
            if (!uploadedTypes.Contains(DocumentType.EmploymentLetter)) missing.Add("Employment letter");
        }
        else if (employmentType == EmploymentType.SelfEmployed || employmentType == EmploymentType.BusinessOwner)
        {
            if (!uploadedTypes.Contains(DocumentType.IncomeTaxReturn)) missing.Add("Income tax return (ITR)");
        }

        if (!uploadedTypes.Contains(DocumentType.BankStatement)) missing.Add("Bank statement");

        if (missing.Any())
            throw new BadRequestException($"Missing required documents: {string.Join(", ", missing)}.");
    }
}
