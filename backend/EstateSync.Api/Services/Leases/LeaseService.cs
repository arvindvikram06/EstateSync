using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using EstateSync.Api.Common;
using EstateSync.Api.DTOs.Leases;
using EstateSync.Api.Exceptions;
using EstateSync.Api.Interfaces.Services.Leases;
using EstateSync.Api.Interfaces.Services.Storage;
using EstateSync.Api.Mappers;
using EstateSync.Api.Models;
using EstateSync.Api.Repositories.Interfaces;

namespace EstateSync.Api.Services.Leases;

public class LeaseService : ILeaseService
{
    private readonly ILeaseRepository _leaseRepo;
    private readonly IRentalApplicationRepository _applicationRepo;
    private readonly IPropertyRepository _propertyRepo;
    private readonly IFileStorageService _fileStorageService;
    private readonly INotificationRepository _notificationRepo;
    private readonly ILeaseBillRepository _leaseBillRepo;
    private readonly IUnitOfWork _unitOfWork;
    private readonly LeaseMapper _mapper;
    private readonly ILogger<LeaseService> _logger;
    private readonly ILeaseDocumentService _leaseDocumentService;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IUserRepository _userRepo;

    public LeaseService(
        ILeaseRepository leaseRepo,
        IRentalApplicationRepository applicationRepo,
        IPropertyRepository propertyRepo,
        IFileStorageService fileStorageService,
        INotificationRepository notificationRepo,
        ILeaseBillRepository leaseBillRepo,
        IUnitOfWork unitOfWork,
        LeaseMapper mapper,
        ILogger<LeaseService> logger,
        ILeaseDocumentService leaseDocumentService,
        IHttpContextAccessor httpContextAccessor,
        IUserRepository userRepo)
    {
        _leaseRepo = leaseRepo ?? throw new ArgumentNullException(nameof(leaseRepo));
        _applicationRepo = applicationRepo ?? throw new ArgumentNullException(nameof(applicationRepo));
        _propertyRepo = propertyRepo ?? throw new ArgumentNullException(nameof(propertyRepo));
        _fileStorageService = fileStorageService ?? throw new ArgumentNullException(nameof(fileStorageService));
        _notificationRepo = notificationRepo ?? throw new ArgumentNullException(nameof(notificationRepo));
        _leaseBillRepo = leaseBillRepo ?? throw new ArgumentNullException(nameof(leaseBillRepo));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _leaseDocumentService = leaseDocumentService ?? throw new ArgumentNullException(nameof(leaseDocumentService));
        _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
        _userRepo = userRepo ?? throw new ArgumentNullException(nameof(userRepo));
    }

    public async Task<LeaseResponse> CreateLeaseAsync(CreateLeaseRequest request, Guid ownerId, CancellationToken ct)
    {
        var application = await _applicationRepo.GetWithDetailsByIdAsync(request.ApplicationId);
        if (application == null)
            throw new NotFoundException("RentalApplication", request.ApplicationId);

        ValidateCreateLeaseRequest(application, ownerId);

        var existingLease = await _leaseRepo.GetByApplicationIdAsync(request.ApplicationId);
        if (existingLease != null && existingLease.Status == LeaseStatus.PendingApproval)
            throw new ConflictException("A lease for this rental application is already pending approval.");

        var property = application.Property!;
        var activeLease = await _leaseRepo.GetActiveLeaseByPropertyIdAsync(property.PropertyId);
        if (activeLease != null && request.StartDate <= activeLease.EndDate)
            throw new ConflictException("The new lease must start after the current active lease ends.");

        var tenantActiveLease = await _leaseRepo.GetActiveLeaseByTenantIdAsync(application.TenantId);
        if (tenantActiveLease != null && request.StartDate <= tenantActiveLease.EndDate)
            throw new ConflictException("Tenant currently has an active lease. The new lease must start after their current lease ends.");

        var leaseId = Guid.NewGuid();

        var lease = new Lease
        {
            LeaseId = leaseId,
            PropertyId = property.PropertyId,
            TenantId = application.TenantId,
            ApplicationId = request.ApplicationId,
            StartDate = DateTime.SpecifyKind(request.StartDate, DateTimeKind.Utc),
            EndDate = DateTime.SpecifyKind(request.EndDate, DateTimeKind.Utc),
            MonthlyRent = request.MonthlyRent,
            SecurityDeposit = request.SecurityDeposit,
            PaymentFrequency = request.PaymentFrequency,
            NoticePeriodDays = request.NoticePeriodDays,
            PenaltyPercentage = request.PenaltyPercentage,
            AgreementText = request.AgreementText,
            Status = LeaseStatus.PendingApproval,
            CreatedAt = DateTime.UtcNow,
            // Set navigation properties so PDF can be generated before saving
            Property = property,
            Tenant = application.Tenant
        };

        var pdfUrl = await _leaseDocumentService.GenerateAgreementPdfAsync(lease, ct);
        lease.SignedAgreementUrl = pdfUrl;

        await _leaseRepo.AddAsync(lease);
        await CreateLeaseNotificationAsync(lease.TenantId, lease.LeaseId, property.Title);
        await _unitOfWork.SaveChangesAsync(ct);

        _logger.LogInformation("Lease {LeaseId} created and agreement PDF generated at {Url}", leaseId, pdfUrl);
        return _mapper.MapToResponse(lease);
    }

    public async Task<LeaseResponse> UpdateLeaseAsync(Guid leaseId, UpdateLeaseRequest request, Guid ownerId, CancellationToken ct)
    {
        var lease = await _leaseRepo.GetWithDetailsByIdAsync(leaseId);
        if (lease == null) throw new NotFoundException("Lease", leaseId);
        if (lease.Property?.OwnerId != ownerId) throw new ForbiddenException("You do not own this property.");
        if (lease.Status != LeaseStatus.PendingApproval)
            throw new BadRequestException("Lease cannot be edited after signing has begun.");

        if (request.StartDate.HasValue) lease.StartDate = DateTime.SpecifyKind(request.StartDate.Value, DateTimeKind.Utc);
        if (request.EndDate.HasValue) lease.EndDate = DateTime.SpecifyKind(request.EndDate.Value, DateTimeKind.Utc);
        if (request.MonthlyRent.HasValue) lease.MonthlyRent = request.MonthlyRent.Value;
        if (request.SecurityDeposit.HasValue) lease.SecurityDeposit = request.SecurityDeposit.Value;
        if (request.PaymentFrequency.HasValue) lease.PaymentFrequency = request.PaymentFrequency.Value;
        if (request.NoticePeriodDays.HasValue) lease.NoticePeriodDays = request.NoticePeriodDays.Value;
        if (request.PenaltyPercentage.HasValue) lease.PenaltyPercentage = request.PenaltyPercentage.Value;
        if (request.AgreementText != null) lease.AgreementText = request.AgreementText;

        // Regenerate unsigned agreement PDF with updated terms
        var pdfUrl = await _leaseDocumentService.GenerateAgreementPdfAsync(lease, ct);
        lease.SignedAgreementUrl = pdfUrl;

        await _unitOfWork.SaveChangesAsync(ct);
        _logger.LogInformation("Lease {LeaseId} updated by owner {OwnerId}", leaseId, ownerId);
        return _mapper.MapToResponse(lease);
    }

    public async Task<LeaseResponse> SignLeaseAsync(Guid leaseId, SignLeaseRequest request, Guid userId, CancellationToken ct)
    {
        var lease = await _leaseRepo.GetWithDetailsByIdAsync(leaseId);
        if (lease == null) throw new NotFoundException("Lease", leaseId);

        if (lease.Status != LeaseStatus.PendingApproval && lease.Status != LeaseStatus.PartialSigned)
            throw new BadRequestException("Lease cannot be signed in its current status.");

        var property = lease.Property ?? throw new NotFoundException("Property", lease.PropertyId);

        bool isTenant = lease.TenantId == userId;
        bool isOwner = property.OwnerId == userId;

        if (!isTenant && !isOwner)
            throw new ForbiddenException("You are not authorized to sign this lease.");

        if (isTenant && lease.TenantSignedAt.HasValue)
            throw new BadRequestException("You have already signed this lease.");
        if (isOwner && lease.OwnerSignedAt.HasValue)
            throw new BadRequestException("You have already signed this lease.");

        var sigBytes = Convert.FromBase64String(request.SignatureImageBase64);
        var role = isTenant ? "tenant" : "owner";
        var sigFileName = $"{role}-sig.png";
        var sigUrl = await _fileStorageService.SaveBytesAsync(sigBytes, $"leases/{leaseId}", sigFileName, ct);
        var clientIp = _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString();

        if (isTenant)
        {
            lease.TenantSignature = request.SignatureText;
            lease.TenantSignedAt = DateTime.UtcNow;
            lease.TenantSignatureImageUrl = sigUrl;
            lease.TenantSignatureIp = clientIp;
            _logger.LogInformation("Lease {LeaseId} signed by tenant {TenantId}", leaseId, userId);
        }
        else
        {
            lease.OwnerSignature = request.SignatureText;
            lease.OwnerSignedAt = DateTime.UtcNow;
            lease.OwnerSignatureImageUrl = sigUrl;
            lease.OwnerSignatureIp = clientIp;
            _logger.LogInformation("Lease {LeaseId} signed by owner {OwnerId}", leaseId, userId);
        }

        bool tenantSigned = lease.TenantSignedAt.HasValue && !string.IsNullOrWhiteSpace(lease.TenantSignature);
        bool ownerSigned = lease.OwnerSignedAt.HasValue && !string.IsNullOrWhiteSpace(lease.OwnerSignature);

        if (tenantSigned && ownerSigned)
        {
            lease.Status = LeaseStatus.PendingAdminApproval;
            lease.SignatureVerificationHash = ComputeVerificationHash(lease, property.OwnerId);

            var signedPdfUrl = await _leaseDocumentService.GenerateSignedAgreementPdfAsync(lease, ct);
            lease.SignedAgreementUrl = signedPdfUrl;

            await NotifyAdminsAsync(leaseId, property.Title, ct);
            _logger.LogInformation("Lease {LeaseId} both parties signed; pending admin approval", leaseId);
        }
        else
        {
            lease.Status = LeaseStatus.PartialSigned;
        }

        await _unitOfWork.SaveChangesAsync(ct);
        return _mapper.MapToResponse(lease);
    }

    public async Task VoidLeaseAsync(Guid leaseId, string? reason, Guid ownerId, CancellationToken ct)
    {
        var lease = await _leaseRepo.GetWithDetailsByIdAsync(leaseId);
        if (lease == null) throw new NotFoundException("Lease", leaseId);
        if (lease.Property?.OwnerId != ownerId) throw new ForbiddenException("You do not own this property.");

        if (lease.Status != LeaseStatus.PendingApproval && lease.Status != LeaseStatus.PartialSigned)
            throw new BadRequestException("Lease can only be voided before admin approval.");

        lease.Status = LeaseStatus.Voided;
        lease.CancelledBy = ownerId;
        lease.CancelledAt = DateTime.UtcNow;
        lease.CancellationReason = reason;

        await _notificationRepo.AddAsync(new Notification
        {
            NotificationId = Guid.NewGuid(),
            UserId = lease.TenantId,
            Type = NotificationType.LeaseVoided,
            Title = "Lease Voided",
            Message = $"The lease for '{lease.Property!.Title}' has been voided by the owner. A new lease will need to be created.",
            EntityId = leaseId.ToString(),
            EntityType = "Lease",
            IsRead = false,
            CreatedAt = DateTime.UtcNow
        });

        await _unitOfWork.SaveChangesAsync(ct);
        _logger.LogInformation("Lease {LeaseId} voided by owner {OwnerId}", leaseId, ownerId);
    }

    public async Task AdminApproveLeaseAsync(Guid leaseId, bool approve, string? remarks, Guid adminId, CancellationToken ct)
    {
        var lease = await _leaseRepo.GetWithDetailsByIdAsync(leaseId);
        if (lease == null) throw new NotFoundException("Lease", leaseId);
        if (lease.Status != LeaseStatus.PendingAdminApproval)
            throw new BadRequestException("Lease is not pending admin approval.");

        var property = lease.Property ?? throw new NotFoundException("Property", lease.PropertyId);

        lease.AdminApprovedById = adminId;
        lease.AdminApprovedAt = DateTime.UtcNow;
        lease.AdminRemarks = remarks;

        if (approve)
        {
            lease.Status = LeaseStatus.Active;
            lease.ActivatedAt = DateTime.UtcNow;
            property.Status = PropertyStatus.Leased;

            await RejectOtherApplicationsOnLeaseActivationAsync(property.PropertyId, lease.ApplicationId, property.Title);
            await GenerateLeaseBillsAsync(lease);

            await NotifyUserAsync(lease.TenantId, leaseId, "Lease Activated",
                $"Your lease for '{property.Title}' has been approved and activated.", NotificationType.LeaseApprovedByAdmin);
            await NotifyUserAsync(property.OwnerId, leaseId, "Lease Activated",
                $"The lease for '{property.Title}' has been approved and activated.", NotificationType.LeaseApprovedByAdmin);

            _logger.LogInformation("Lease {LeaseId} approved by admin {AdminId}", leaseId, adminId);
        }
        else
        {
            lease.Status = LeaseStatus.Voided;
            lease.CancelledAt = DateTime.UtcNow;
            lease.CancellationReason = remarks;

            await NotifyUserAsync(lease.TenantId, leaseId, "Lease Rejected",
                $"Your lease for '{property.Title}' was rejected by admin. Reason: {remarks}", NotificationType.LeaseRejectedByAdmin);
            await NotifyUserAsync(property.OwnerId, leaseId, "Lease Rejected",
                $"The lease for '{property.Title}' was rejected by admin. Reason: {remarks}", NotificationType.LeaseRejectedByAdmin);

            _logger.LogInformation("Lease {LeaseId} rejected by admin {AdminId}", leaseId, adminId);
        }

        await _unitOfWork.SaveChangesAsync(ct);
    }

    public async Task<LeaseResponse> GetLeaseByIdAsync(Guid leaseId, Guid userId, bool isAdmin, CancellationToken ct)
    {
        var lease = await _leaseRepo.GetWithDetailsByIdAsync(leaseId);
        if (lease == null) throw new NotFoundException("Lease", leaseId);

        var property = lease.Property;
        bool isTenant = lease.TenantId == userId;
        bool isOwner = property != null && property.OwnerId == userId;

        if (!isTenant && !isOwner && !isAdmin)
            throw new ForbiddenException("You are not authorized to view this lease.");

        return _mapper.MapToResponse(lease);
    }

    public async Task<PagedResponse<LeaseResponse>> GetTenantLeasesAsync(Guid tenantId, int page, int limit, CancellationToken ct)
    {
        var paged = await _leaseRepo.GetByTenantIdPagedAsync(tenantId, page, limit);
        return new PagedResponse<LeaseResponse>
        {
            Success = true,
            Data = paged.Items.Select(_mapper.MapToResponse),
            Page = page,
            Limit = limit,
            Total = paged.Total
        };
    }

    public async Task<PagedResponse<LeaseResponse>> GetOwnerLeasesAsync(Guid ownerId, int page, int limit, CancellationToken ct)
    {
        var paged = await _leaseRepo.GetByOwnerIdPagedAsync(ownerId, page, limit);
        return new PagedResponse<LeaseResponse>
        {
            Success = true,
            Data = paged.Items.Select(_mapper.MapToResponse),
            Page = page,
            Limit = limit,
            Total = paged.Total
        };
    }

    public async Task<PagedResponse<LeaseResponse>> GetAllLeasesAsync(int page, int limit, CancellationToken ct)
    {
        var paged = await _leaseRepo.GetPagedAsync(page, limit);
        return new PagedResponse<LeaseResponse>
        {
            Success = true,
            Data = paged.Items.Select(_mapper.MapToResponse),
            Page = page,
            Limit = limit,
            Total = paged.Total
        };
    }

    public async Task RequestRenewalAsync(Guid leaseId, Guid tenantId, CancellationToken ct)
    {
        var lease = await _leaseRepo.GetWithDetailsByIdAsync(leaseId);
        if (lease == null) throw new NotFoundException("Lease", leaseId);
        if (lease.TenantId != tenantId) throw new ForbiddenException("You are not authorized to renew this lease.");
        if (lease.Status != LeaseStatus.Active) throw new BadRequestException("Only active leases can be renewed.");

        lease.IsRenewalRequested = true;

        await _notificationRepo.AddAsync(new Notification
        {
            NotificationId = Guid.NewGuid(),
            UserId = lease.Property!.OwnerId,
            Type = NotificationType.General,
            Title = "Lease Renewal Requested",
            Message = $"Your tenant has requested to renew the lease for '{lease.Property.Title}'.",
            EntityId = lease.LeaseId.ToString(),
            EntityType = "Lease",
            CreatedAt = DateTime.UtcNow
        });

        await _unitOfWork.SaveChangesAsync(ct);
    }

    public async Task RejectRenewalAsync(Guid leaseId, Guid ownerId, CancellationToken ct)
    {
        var lease = await _leaseRepo.GetWithDetailsByIdAsync(leaseId);
        if (lease == null) throw new NotFoundException("Lease", leaseId);
        if (lease.Property?.OwnerId != ownerId) throw new ForbiddenException("You do not own this property.");
        if (!lease.IsRenewalRequested) throw new BadRequestException("Renewal has not been requested.");

        lease.IsRenewalRequested = false;

        await _notificationRepo.AddAsync(new Notification
        {
            NotificationId = Guid.NewGuid(),
            UserId = lease.TenantId,
            Type = NotificationType.General,
            Title = "Lease Renewal Rejected",
            Message = $"Your request to renew the lease for '{lease.Property!.Title}' was rejected.",
            EntityId = lease.LeaseId.ToString(),
            EntityType = "Lease",
            CreatedAt = DateTime.UtcNow
        });

        await _unitOfWork.SaveChangesAsync(ct);
    }

    public async Task TerminateLeaseAsync(Guid leaseId, Guid requesterId, bool isAdmin, string reason, CancellationToken ct)
    {
        var lease = await _leaseRepo.GetWithDetailsByIdAsync(leaseId);
        if (lease == null) throw new NotFoundException("Lease", leaseId);

        var property = lease.Property ?? throw new NotFoundException("Property", lease.PropertyId);

        if (!isAdmin && property.OwnerId != requesterId)
            throw new ForbiddenException("You do not own this property.");

        if (lease.Status != LeaseStatus.Active)
            throw new BadRequestException("Only active leases can be terminated.");

        lease.Status = LeaseStatus.Terminated;
        lease.CancelledBy = requesterId;
        lease.CancelledAt = DateTime.UtcNow;
        lease.CancellationReason = reason;
        lease.IsDepositForfeited = true;

        if (property.Status == PropertyStatus.Leased || property.Status == PropertyStatus.AvailableSoon)
            property.Status = PropertyStatus.Active;

        await _notificationRepo.AddAsync(new Notification
        {
            NotificationId = Guid.NewGuid(),
            UserId = lease.TenantId,
            Type = NotificationType.DepositForfeited,
            Title = "Lease Terminated — Deposit Forfeited",
            Message = $"Your lease for '{property.Title}' has been terminated. As per lease terms, your security deposit has been forfeited.",
            EntityId = leaseId.ToString(),
            EntityType = "Lease",
            IsRead = false,
            CreatedAt = DateTime.UtcNow
        });

        await _notificationRepo.AddAsync(new Notification
        {
            NotificationId = Guid.NewGuid(),
            UserId = property.OwnerId,
            Type = NotificationType.LeaseTerminated,
            Title = "Lease Terminated",
            Message = $"The lease for '{property.Title}' has been terminated. The security deposit has been retained as per lease terms.",
            EntityId = leaseId.ToString(),
            EntityType = "Lease",
            IsRead = false,
            CreatedAt = DateTime.UtcNow
        });

        await _unitOfWork.SaveChangesAsync(ct);
        _logger.LogInformation("Lease {LeaseId} terminated by {RequesterId}", leaseId, requesterId);
    }

    private void ValidateCreateLeaseRequest(RentalApplication application, Guid ownerId)
    {
        if (application.Status != ApplicationStatus.Approved)
            throw new BadRequestException("Rental application is not approved.");

        if (application.Tenant?.IsKycVerified != true)
            throw new BadRequestException("Tenant has not completed KYC verification.");

        if (application.Property == null)
            throw new NotFoundException("Property for application", application.PropertyId);

        if (application.Property.OwnerId != ownerId)
            throw new ForbiddenException("You do not own this property.");
    }

    private async Task CreateLeaseNotificationAsync(Guid tenantId, Guid leaseId, string propertyTitle)
    {
        await _notificationRepo.AddAsync(new Notification
        {
            NotificationId = Guid.NewGuid(),
            UserId = tenantId,
            Type = NotificationType.LeaseCreated,
            Title = "New Lease Agreement Created",
            Message = $"A lease agreement for '{propertyTitle}' has been created and is waiting for your signature.",
            EntityId = leaseId.ToString(),
            EntityType = "Lease",
            IsRead = false,
            CreatedAt = DateTime.UtcNow
        });
    }

    private async Task NotifyUserAsync(Guid userId, Guid leaseId, string title, string message, NotificationType type)
    {
        await _notificationRepo.AddAsync(new Notification
        {
            NotificationId = Guid.NewGuid(),
            UserId = userId,
            Type = type,
            Title = title,
            Message = message,
            EntityId = leaseId.ToString(),
            EntityType = "Lease",
            IsRead = false,
            CreatedAt = DateTime.UtcNow
        });
    }

    private async Task NotifyAdminsAsync(Guid leaseId, string propertyTitle, CancellationToken ct)
    {
        var admins = await _userRepo.GetUsersByRoleAsync("Admin");
        foreach (var admin in admins)
        {
            await _notificationRepo.AddAsync(new Notification
            {
                NotificationId = Guid.NewGuid(),
                UserId = admin.UserId,
                Type = NotificationType.LeasePendingAdminApproval,
                Title = "Lease Pending Approval",
                Message = $"A lease for '{propertyTitle}' has been signed by both parties and requires your approval.",
                EntityId = leaseId.ToString(),
                EntityType = "Lease",
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            });
        }
    }

    private async Task GenerateLeaseBillsAsync(Lease lease)
    {
        if (lease.SecurityDeposit > 0)
        {
            await _leaseBillRepo.AddAsync(new LeaseBill
            {
                LeaseBillId = Guid.NewGuid(),
                LeaseId = lease.LeaseId,
                BillType = LeaseBillType.Deposit,
                DueDate = lease.StartDate,
                Amount = lease.SecurityDeposit,
                PendingAmount = lease.SecurityDeposit,
                CreatedAt = DateTime.UtcNow
            });
        }

        int multiplier = lease.PaymentFrequency switch
        {
            PaymentFrequency.Monthly => 1,
            PaymentFrequency.Quarterly => 3,
            PaymentFrequency.HalfYearly => 6,
            PaymentFrequency.Yearly => 12,
            _ => 1
        };

        await _leaseBillRepo.AddAsync(new LeaseBill
        {
            LeaseBillId = Guid.NewGuid(),
            LeaseId = lease.LeaseId,
            BillType = LeaseBillType.Rent,
            DueDate = lease.StartDate,
            Amount = lease.MonthlyRent * multiplier,
            PendingAmount = lease.MonthlyRent * multiplier,
            CreatedAt = DateTime.UtcNow
        });

        _logger.LogInformation("Generated bills for lease {LeaseId}", lease.LeaseId);
    }

    private async Task RejectOtherApplicationsOnLeaseActivationAsync(Guid propertyId, Guid currentApplicationId, string propertyTitle)
    {
        var applications = await _applicationRepo.GetByPropertyIdAsync(propertyId);
        foreach (var app in applications)
        {
            if (app.ApplicationId == currentApplicationId) continue;
            if (app.Status != ApplicationStatus.Pending && app.Status != ApplicationStatus.Approved) continue;

            app.Status = ApplicationStatus.Rejected;
            app.ReviewRemarks = "Property is reserved";
            app.ReviewedAt = DateTime.UtcNow;

            await _notificationRepo.AddAsync(new Notification
            {
                NotificationId = Guid.NewGuid(),
                UserId = app.TenantId,
                Type = NotificationType.ApplicationRejected,
                Title = "Application Rejected",
                Message = $"Your application for property '{propertyTitle}' was rejected. Reason: Property is reserved",
                EntityId = app.ApplicationId.ToString(),
                EntityType = "RentalApplication",
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            });
        }
    }

    private static string ComputeVerificationHash(Lease lease, Guid ownerId)
    {
        var raw = $"{lease.LeaseId}{lease.TenantId}{ownerId}{lease.TenantSignedAt:O}{lease.OwnerSignedAt:O}";
        var hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(raw));
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }
}
