using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using EstateSync.Api.Common;
using EstateSync.Api.DTOs.Leases;
using EstateSync.Api.Exceptions;
using EstateSync.Api.Interfaces.Services.Leases;
using EstateSync.Api.Interfaces.Services.Notifications;
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
    private readonly INotificationSender _notificationSender;
    private readonly ILeaseBillRepository _leaseBillRepo;
    private readonly IPaymentRepository _paymentRepo;
    private readonly IUnitOfWork _unitOfWork;
    private readonly LeaseMapper _mapper;
    private readonly ILogger<LeaseService> _logger;
    private readonly ILeaseDocumentService _leaseDocumentService;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IUserRepository _userRepo;
    private readonly ILeaseVacateNoticeRepository _vacateNoticeRepo;

    public LeaseService(
        ILeaseRepository leaseRepo,
        IRentalApplicationRepository applicationRepo,
        IPropertyRepository propertyRepo,
        IFileStorageService fileStorageService,
        INotificationSender notificationSender,
        ILeaseBillRepository leaseBillRepo,
        IPaymentRepository paymentRepo,
        IUnitOfWork unitOfWork,
        LeaseMapper mapper,
        ILogger<LeaseService> logger,
        ILeaseDocumentService leaseDocumentService,
        IHttpContextAccessor httpContextAccessor,
        IUserRepository userRepo,
        ILeaseVacateNoticeRepository vacateNoticeRepo)
    {
        _leaseRepo = leaseRepo ?? throw new ArgumentNullException(nameof(leaseRepo));
        _applicationRepo = applicationRepo ?? throw new ArgumentNullException(nameof(applicationRepo));
        _propertyRepo = propertyRepo ?? throw new ArgumentNullException(nameof(propertyRepo));
        _fileStorageService = fileStorageService ?? throw new ArgumentNullException(nameof(fileStorageService));
        _notificationSender = notificationSender ?? throw new ArgumentNullException(nameof(notificationSender));
        _leaseBillRepo = leaseBillRepo ?? throw new ArgumentNullException(nameof(leaseBillRepo));
        _paymentRepo = paymentRepo ?? throw new ArgumentNullException(nameof(paymentRepo));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _leaseDocumentService = leaseDocumentService ?? throw new ArgumentNullException(nameof(leaseDocumentService));
        _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
        _userRepo = userRepo ?? throw new ArgumentNullException(nameof(userRepo));
        _vacateNoticeRepo = vacateNoticeRepo ?? throw new ArgumentNullException(nameof(vacateNoticeRepo));
    }

    public async Task<LeaseResponse> CreateLeaseAsync(CreateLeaseRequest request, Guid ownerId, CancellationToken ct)
    {
        var application = await _applicationRepo.GetWithDetailsByIdAsync(request.ApplicationId);
        if (application == null)
            throw new NotFoundException("RentalApplication", request.ApplicationId);

        ValidateCreateLeaseRequest(application, ownerId);

        var existingLease = await _leaseRepo.GetByApplicationIdAsync(request.ApplicationId);
        if (existingLease != null &&
            existingLease.Status != LeaseStatus.Voided &&
            existingLease.Status != LeaseStatus.Expired &&
            existingLease.Status != LeaseStatus.Terminated)
        {
            var statusLabel = existingLease.Status switch
            {
                LeaseStatus.PendingApproval      => "pending signature",
                LeaseStatus.PartialSigned         => "partially signed",
                LeaseStatus.PendingAdminApproval  => "awaiting admin approval",
                LeaseStatus.Active                => "active",
                _                                 => existingLease.Status.ToString()
            };
            throw new ConflictException($"A lease for this application already exists ({statusLabel}). Void or wait for it to complete before creating a new one.");
        }

        var property = application.Property!;
        var (activeLease, tenantActiveLease) = await _leaseRepo.GetActiveLeaseConflictsAsync(property.PropertyId, application.TenantId);
        if (activeLease != null && request.StartDate <= activeLease.EndDate)
            throw new ConflictException("The new lease must start after the current active lease ends.");

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
        try
        {
            await _unitOfWork.SaveChangesAsync(ct);
        }
        // Concurrent create slipped past the read-then-check guard above; the
        // partial unique index on Leases.ApplicationId rejects the duplicate.
        catch (Microsoft.EntityFrameworkCore.DbUpdateException ex)
            when (ex.InnerException is Npgsql.PostgresException { SqlState: "23505" })
        {
            throw new ConflictException("A lease for this application already exists.");
        }

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

        await _notificationSender.SendAsync(new Notification
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
        var lease = await GetAuthorizedLeaseAsync(leaseId, userId, isAdmin);
        return _mapper.MapToResponse(lease);
    }

    public async Task<List<LeaseBillDetailResponse>> GetLeaseBillsAsync(Guid leaseId, Guid userId, bool isAdmin, CancellationToken ct)
    {
        await GetAuthorizedLeaseAsync(leaseId, userId, isAdmin);

        var bills = await _leaseBillRepo.GetByLeaseIdAsync(leaseId);
        return bills
            .OrderByDescending(b => b.DueDate)
            .Select(b => new LeaseBillDetailResponse
            {
                LeaseBillId = b.LeaseBillId,
                BillType = b.BillType,
                DueDate = b.DueDate,
                Amount = b.Amount,
                PendingAmount = b.PendingAmount,
            })
            .ToList();
    }

    public async Task<List<PaymentRecordResponse>> GetLeasePaymentsAsync(Guid leaseId, Guid userId, bool isAdmin, CancellationToken ct)
    {
        await GetAuthorizedLeaseAsync(leaseId, userId, isAdmin);

        var payments = await _paymentRepo.GetByLeaseIdAsync(leaseId, ct);
        return payments
            .Select(p => new PaymentRecordResponse
            {
                PaymentId = p.PaymentId,
                LeaseBillId = p.LeaseBillId,
                BillType = p.LeaseBill?.BillType ?? LeaseBillType.Other,
                Amount = p.Amount,
                PlatformFee = p.PlatformFee,
                TransactionId = p.TransactionId,
                PaymentStatus = p.PaymentStatus.ToString(),
                PaidDate = p.PaidDate,
            })
            .ToList();
    }

    /// <summary>Loads a lease and ensures the caller is its tenant, the property owner, or an admin.</summary>
    private async Task<Lease> GetAuthorizedLeaseAsync(Guid leaseId, Guid userId, bool isAdmin)
    {
        var lease = await _leaseRepo.GetWithDetailsByIdAsync(leaseId);
        if (lease == null) throw new NotFoundException("Lease", leaseId);

        bool isTenant = lease.TenantId == userId;
        bool isOwner = lease.Property != null && lease.Property.OwnerId == userId;

        if (!isTenant && !isOwner && !isAdmin)
            throw new ForbiddenException("You are not authorized to view this lease.");

        return lease;
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
        var paged = await _leaseRepo.GetAllPagedAsync(page, limit);
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

        await _notificationSender.SendAsync(new Notification
        {
            NotificationId = Guid.NewGuid(),
            UserId = lease.Property!.OwnerId,
            Type = NotificationType.LeaseRenewalRequested,
            Title = "Lease Renewal Requested",
            Message = $"Your tenant has requested to renew the lease for '{lease.Property.Title}'.",
            EntityId = lease.LeaseId.ToString(),
            EntityType = "Lease",
            CreatedAt = DateTime.UtcNow
        });

        await _unitOfWork.SaveChangesAsync(ct);
    }

    public async Task ApproveRenewalAsync(Guid leaseId, Guid ownerId, DateTime newEndDate, CancellationToken ct)
    {
        var lease = await _leaseRepo.GetWithDetailsByIdAsync(leaseId);
        if (lease == null) throw new NotFoundException("Lease", leaseId);
        if (lease.Property?.OwnerId != ownerId) throw new ForbiddenException("You do not own this property.");
        if (!lease.IsRenewalRequested) throw new BadRequestException("Renewal has not been requested.");
        if (lease.Status != LeaseStatus.Active) throw new BadRequestException("Only active leases can be renewed.");

        var utcNewEndDate = DateTime.SpecifyKind(newEndDate, DateTimeKind.Utc);
        if (utcNewEndDate <= lease.EndDate)
            throw new BadRequestException("New end date must be after the current lease end date.");

        lease.EndDate = utcNewEndDate;
        lease.IsRenewalRequested = false;

        // Undo the expiry scheduler's 3-month warning state, if it fired
        if (lease.Property.Status == PropertyStatus.AvailableSoon)
            lease.Property.Status = PropertyStatus.Leased;

        await _notificationSender.SendAsync(new Notification
        {
            NotificationId = Guid.NewGuid(),
            UserId = lease.TenantId,
            Type = NotificationType.LeaseRenewalApproved,
            Title = "Lease Renewal Approved",
            Message = $"Your lease for '{lease.Property.Title}' has been renewed until {lease.EndDate:d MMM yyyy}.",
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

        await _notificationSender.SendAsync(new Notification
        {
            NotificationId = Guid.NewGuid(),
            UserId = lease.TenantId,
            Type = NotificationType.LeaseRenewalRejected,
            Title = "Lease Renewal Rejected",
            Message = $"Your request to renew the lease for '{lease.Property!.Title}' was rejected.",
            EntityId = lease.LeaseId.ToString(),
            EntityType = "Lease",
            CreatedAt = DateTime.UtcNow
        });

        await _unitOfWork.SaveChangesAsync(ct);
    }

    public async Task TerminateLeaseAsync(Guid leaseId, Guid requesterId, bool isAdmin, string reason, bool forfeitDeposit, CancellationToken ct)
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
        lease.IsDepositForfeited = forfeitDeposit;

        // Close out the application so it cannot be used to create another lease.
        var application = await _applicationRepo.GetByIdAsync(lease.ApplicationId);
        if (application != null && application.Status == ApplicationStatus.Approved)
            application.Status = ApplicationStatus.Completed;

        if (property.Status == PropertyStatus.Leased || property.Status == PropertyStatus.AvailableSoon)
            property.Status = PropertyStatus.Active;

        await _notificationSender.SendAsync(new Notification
        {
            NotificationId = Guid.NewGuid(),
            UserId = lease.TenantId,
            Type = forfeitDeposit ? NotificationType.DepositForfeited : NotificationType.LeaseTerminated,
            Title = forfeitDeposit ? "Lease Terminated — Deposit Forfeited" : "Lease Terminated",
            Message = forfeitDeposit
                ? $"Your lease for '{property.Title}' has been terminated. As per lease terms, your security deposit has been forfeited."
                : $"Your lease for '{property.Title}' has been terminated. Your security deposit will be returned by the owner.",
            EntityId = leaseId.ToString(),
            EntityType = "Lease",
            IsRead = false,
            CreatedAt = DateTime.UtcNow
        });

        await _notificationSender.SendAsync(new Notification
        {
            NotificationId = Guid.NewGuid(),
            UserId = property.OwnerId,
            Type = NotificationType.LeaseTerminated,
            Title = "Lease Terminated",
            Message = forfeitDeposit
                ? $"The lease for '{property.Title}' has been terminated. The security deposit has been retained as per lease terms."
                : $"The lease for '{property.Title}' has been terminated. Please arrange the return of the tenant's security deposit.",
            EntityId = leaseId.ToString(),
            EntityType = "Lease",
            IsRead = false,
            CreatedAt = DateTime.UtcNow
        });

        await _unitOfWork.SaveChangesAsync(ct);
        _logger.LogInformation("Lease {LeaseId} terminated by {RequesterId}", leaseId, requesterId);
    }

    public async Task<LeaseVacateNoticeResponse?> GetPendingVacateNoticeAsync(Guid leaseId, Guid userId, bool isAdmin, CancellationToken ct)
    {
        var lease = await _leaseRepo.GetWithDetailsByIdAsync(leaseId);
        if (lease == null) throw new NotFoundException("Lease", leaseId);

        if (!isAdmin && lease.TenantId != userId && lease.Property?.OwnerId != userId)
            throw new ForbiddenException("You are not authorized to view this lease.");

        var notice = await _vacateNoticeRepo.GetPendingByLeaseIdAsync(leaseId);
        return notice == null ? null : MapVacateNoticeToResponse(notice);
    }

    public async Task<LeaseVacateNoticeResponse> SubmitVacateNoticeAsync(Guid leaseId, Guid tenantId, DateTime requestedExitDate, CancellationToken ct)
    {
        var lease = await _leaseRepo.GetWithDetailsByIdAsync(leaseId);
        if (lease == null) throw new NotFoundException("Lease", leaseId);

        if (lease.TenantId != tenantId)
            throw new ForbiddenException("You are not the tenant of this lease.");

        if (lease.Status != LeaseStatus.Active)
            throw new BadRequestException("Only active leases can have a vacate notice submitted.");

        if (requestedExitDate.Date <= DateTime.UtcNow.Date)
            throw new BadRequestException("Requested exit date must be in the future.");

        var existing = await _vacateNoticeRepo.GetPendingByLeaseIdAsync(leaseId);
        if (existing != null)
            throw new ConflictException("A pending vacate notice already exists for this lease.");

        var noticePeriodDays = lease.NoticePeriodDays ?? 0;
        var penaltyApplied = requestedExitDate.Date < DateTime.UtcNow.AddDays(noticePeriodDays).Date;
        var penaltyAmount = penaltyApplied && lease.PenaltyPercentage.HasValue && lease.PenaltyPercentage > 0
            ? Math.Round(lease.SecurityDeposit * lease.PenaltyPercentage.Value / 100, 2)
            : 0m;

        var notice = new LeaseVacateNotice
        {
            VacateNoticeId = Guid.NewGuid(),
            LeaseId = leaseId,
            TenantId = tenantId,
            RequestedExitDate = requestedExitDate,
            Status = VacateNoticeStatus.Pending,
            PenaltyApplied = penaltyApplied,
            PenaltyAmount = penaltyAmount
        };
        await _vacateNoticeRepo.AddAsync(notice);

        var property = lease.Property!;

        // Exit is confirmed with no owner action needed — signal property is coming available
        if (!penaltyApplied && property.Status == PropertyStatus.Leased)
            property.Status = PropertyStatus.AvailableSoon;

        await _notificationSender.SendAsync(new Notification
        {
            NotificationId = Guid.NewGuid(),
            UserId = property.OwnerId,
            Type = NotificationType.VacateNoticeSubmitted,
            Title = "Tenant Submitted Vacate Notice",
            Message = penaltyApplied
                ? $"Your tenant has submitted a vacate notice for '{property.Title}' with exit date {requestedExitDate:dd MMM yyyy}. A penalty of ₹{penaltyAmount:N0} will apply if you accept."
                : $"Your tenant has submitted a vacate notice for '{property.Title}' with exit date {requestedExitDate:dd MMM yyyy}.",
            EntityId = notice.VacateNoticeId.ToString(),
            EntityType = "LeaseVacateNotice",
            IsRead = false,
            CreatedAt = DateTime.UtcNow
        });

        await _unitOfWork.SaveChangesAsync(ct);
        _logger.LogInformation("Vacate notice {NoticeId} submitted for lease {LeaseId} by tenant {TenantId}", notice.VacateNoticeId, leaseId, tenantId);

        return MapVacateNoticeToResponse(notice);
    }

    public async Task AcceptVacateNoticeAsync(Guid vacateNoticeId, Guid ownerId, CancellationToken ct)
    {
        var notice = await _vacateNoticeRepo.GetByIdWithDetailsAsync(vacateNoticeId);
        if (notice == null) throw new NotFoundException("VacateNotice", vacateNoticeId);

        var lease = notice.Lease ?? throw new NotFoundException("Lease", notice.LeaseId);
        var property = lease.Property ?? throw new NotFoundException("Property", lease.PropertyId);

        if (property.OwnerId != ownerId)
            throw new ForbiddenException("You do not own this property.");

        if (notice.Status != VacateNoticeStatus.Pending)
            throw new BadRequestException("This vacate notice has already been responded to.");

        notice.Status = VacateNoticeStatus.Accepted;

        // Only now that the owner has accepted: raise the penalty bill if applicable
        if (notice.PenaltyApplied && notice.PenaltyAmount > 0)
        {
            await _leaseBillRepo.AddAsync(new LeaseBill
            {
                LeaseBillId = Guid.NewGuid(),
                LeaseId = lease.LeaseId,
                BillType = LeaseBillType.Penalty,
                Amount = notice.PenaltyAmount,
                PendingAmount = notice.PenaltyAmount,
                DueDate = notice.RequestedExitDate
            });
        }

        // Lease stays Active until RequestedExitDate — scheduler terminates on that date
        if (property.Status == PropertyStatus.Leased)
            property.Status = PropertyStatus.AvailableSoon;

        await _notificationSender.SendAsync(new Notification
        {
            NotificationId = Guid.NewGuid(),
            UserId = lease.TenantId,
            Type = NotificationType.VacateNoticeAccepted,
            Title = "Vacate Notice Accepted",
            Message = notice.PenaltyApplied
                ? $"Your vacate notice for '{property.Title}' has been accepted. A penalty of ₹{notice.PenaltyAmount:N0} has been billed. Your lease will end on {notice.RequestedExitDate:dd MMM yyyy}."
                : $"Your vacate notice for '{property.Title}' has been accepted. Your lease will end on {notice.RequestedExitDate:dd MMM yyyy}.",
            EntityId = notice.VacateNoticeId.ToString(),
            EntityType = "LeaseVacateNotice",
            IsRead = false,
            CreatedAt = DateTime.UtcNow
        });

        await _unitOfWork.SaveChangesAsync(ct);
        _logger.LogInformation("Vacate notice {NoticeId} accepted. Lease {LeaseId} will terminate on {ExitDate}.", vacateNoticeId, lease.LeaseId, notice.RequestedExitDate);
    }

    public async Task RejectVacateNoticeAsync(Guid vacateNoticeId, Guid ownerId, string reason, CancellationToken ct)
    {
        var notice = await _vacateNoticeRepo.GetByIdWithDetailsAsync(vacateNoticeId);
        if (notice == null) throw new NotFoundException("VacateNotice", vacateNoticeId);

        var lease = notice.Lease ?? throw new NotFoundException("Lease", notice.LeaseId);
        var property = lease.Property ?? throw new NotFoundException("Property", lease.PropertyId);

        if (property.OwnerId != ownerId)
            throw new ForbiddenException("You do not own this property.");

        if (notice.Status != VacateNoticeStatus.Pending)
            throw new BadRequestException("This vacate notice has already been responded to.");

        notice.Status = VacateNoticeStatus.Rejected;
        notice.RejectionReason = reason;

        await _notificationSender.SendAsync(new Notification
        {
            NotificationId = Guid.NewGuid(),
            UserId = lease.TenantId,
            Type = NotificationType.VacateNoticeRejected,
            Title = "Vacate Notice Rejected",
            Message = $"Your vacate notice for '{property.Title}' was rejected. Reason: {reason}",
            EntityId = notice.VacateNoticeId.ToString(),
            EntityType = "LeaseVacateNotice",
            IsRead = false,
            CreatedAt = DateTime.UtcNow
        });

        await _unitOfWork.SaveChangesAsync(ct);
        _logger.LogInformation("Vacate notice {NoticeId} rejected by owner {OwnerId}", vacateNoticeId, ownerId);
    }

    private static LeaseVacateNoticeResponse MapVacateNoticeToResponse(LeaseVacateNotice notice) => new()
    {
        VacateNoticeId = notice.VacateNoticeId,
        LeaseId = notice.LeaseId,
        RequestedExitDate = notice.RequestedExitDate,
        Status = notice.Status.ToString(),
        PenaltyApplied = notice.PenaltyApplied,
        PenaltyAmount = notice.PenaltyAmount,
        RejectionReason = notice.RejectionReason,
        CreatedAt = notice.CreatedAt
    };

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
        await _notificationSender.SendAsync(new Notification
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
        await _notificationSender.SendAsync(new Notification
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
        var admins = await _userRepo.GetUsersByRoleAsync(Roles.Admin);
        foreach (var admin in admins)
        {
            await _notificationSender.SendAsync(new Notification
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
        var applications = await _applicationRepo.GetByPropertyIdForUpdateAsync(propertyId);
        foreach (var app in applications)
        {
            if (app.ApplicationId == currentApplicationId) continue;
            if (app.Status != ApplicationStatus.Pending && app.Status != ApplicationStatus.Approved) continue;

            app.Status = ApplicationStatus.Rejected;
            app.ReviewRemarks = "Property is reserved";
            app.ReviewedAt = DateTime.UtcNow;

            await _notificationSender.SendAsync(new Notification
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

}
