using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
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

    public LeaseService(
        ILeaseRepository leaseRepo,
        IRentalApplicationRepository applicationRepo,
        IPropertyRepository propertyRepo,
        IFileStorageService fileStorageService,
        INotificationRepository notificationRepo,
        ILeaseBillRepository leaseBillRepo,
        IUnitOfWork unitOfWork,
        LeaseMapper mapper,
        ILogger<LeaseService> logger)
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
    }

    public async Task<LeaseResponse> CreateLeaseAsync(CreateLeaseRequest request, Guid ownerId, CancellationToken ct)
    {
        var application = await _applicationRepo.GetWithDetailsByIdAsync(request.ApplicationId);
        if (application == null)
        {
            throw new NotFoundException("RentalApplication", request.ApplicationId);
        }

        ValidateCreateLeaseRequest(application, ownerId);

        var property = application.Property!;
        var activeLease = await _leaseRepo.GetActiveLeaseByPropertyIdAsync(property.PropertyId);
        if (activeLease != null)
        {
            throw new ConflictException("An active lease already exists for this property.");
        }

        var unsignedAgreementUrl = await ProcessUnsignedAgreementAsync(property.PropertyId, request.UnsignedAgreement, ct);

        var lease = CreateLeaseEntity(request, property.PropertyId, application.TenantId, unsignedAgreementUrl);
        await _leaseRepo.AddAsync(lease);

        await CreateAndSendLeaseNotificationAsync(lease.TenantId, lease.LeaseId, property.Title);
        await _unitOfWork.SaveChangesAsync(ct);

        _logger.LogInformation("Lease {LeaseId} created for property {PropertyId} by owner {OwnerId}", lease.LeaseId, property.PropertyId, ownerId);

        var createdLease = await _leaseRepo.GetWithDetailsByIdAsync(lease.LeaseId);
        return _mapper.MapToResponse(createdLease ?? lease);
    }

    public async Task<LeaseResponse> SignLeaseAsync(Guid leaseId, SignLeaseRequest request, Guid userId, CancellationToken ct)
    {
        var lease = await _leaseRepo.GetWithDetailsByIdAsync(leaseId);
        if (lease == null)
        {
            throw new NotFoundException("Lease", leaseId);
        }

        if (lease.Status != LeaseStatus.PendingApproval)
        {
            throw new BadRequestException("Lease is not in PendingApproval status and cannot be signed.");
        }

        var property = lease.Property;
        if (property == null)
        {
            throw new NotFoundException("Property", lease.PropertyId);
        }

        bool isTenant = lease.TenantId == userId;
        bool isOwner = property.OwnerId == userId;

        if (!isTenant && !isOwner)
        {
            throw new ForbiddenException("You are not authorized to sign this lease.");
        }

        if (isTenant && request.SignedAgreementFile == null)
        {
            throw new BadRequestException("Tenant must upload the signed agreement document.");
        }

        var signedAgreementUrl = await ProcessSignedAgreementAsync(property.PropertyId, request.SignedAgreementFile, ct);
        UpdateLeaseSignature(lease, request.SignatureText, isTenant, signedAgreementUrl);

        if (lease.TenantSignedAt.HasValue && !string.IsNullOrWhiteSpace(lease.TenantSignature) &&
            lease.OwnerSignedAt.HasValue && !string.IsNullOrWhiteSpace(lease.OwnerSignature))
        {
            lease.Status = LeaseStatus.Active;
            lease.ActivatedAt = DateTime.UtcNow;

            property.Status = PropertyStatus.Leased;
            // await _propertyRepo.UpdateAsync(property);

            await RejectOtherApplicationsOnLeaseActivationAsync(property.PropertyId, lease.ApplicationId, property.Title);

            // 1. Generate Security Deposit Bill
            if (lease.SecurityDeposit > 0)
            {
                var depositBill = new LeaseBill
                {
                    LeaseBillId = Guid.NewGuid(),
                    LeaseId = lease.LeaseId,
                    BillType = LeaseBillType.Deposit,
                    DueDate = lease.StartDate,
                    Amount = lease.SecurityDeposit,
                    PendingAmount = lease.SecurityDeposit,
                    CreatedAt = DateTime.UtcNow
                };
                await _leaseBillRepo.AddAsync(depositBill);
                _logger.LogInformation("Generated initial Security Deposit bill for Lease {LeaseId}", lease.LeaseId);
            }

            // 2. Generate First Rent Bill based on payment frequency
            int multiplier = lease.PaymentFrequency switch
            {
                PaymentFrequency.Monthly => 1,
                PaymentFrequency.Quarterly => 3,
                PaymentFrequency.HalfYearly => 6,
                PaymentFrequency.Yearly => 12,
                _ => 1
            };

            var rentBill = new LeaseBill
            {
                LeaseBillId = Guid.NewGuid(),
                LeaseId = lease.LeaseId,
                BillType = LeaseBillType.Rent,
                DueDate = lease.StartDate,
                Amount = lease.MonthlyRent * multiplier,
                PendingAmount = lease.MonthlyRent * multiplier,
                CreatedAt = DateTime.UtcNow
            };
            await _leaseBillRepo.AddAsync(rentBill);
            _logger.LogInformation("Generated first {Frequency} Rent bill for Lease {LeaseId}", lease.PaymentFrequency, lease.LeaseId);

            _logger.LogInformation("Lease {LeaseId} automatically activated as both parties have signed", leaseId);
        }

        // await _leaseRepo.UpdateAsync(lease);
        await _unitOfWork.SaveChangesAsync(ct);

        return _mapper.MapToResponse(lease);
    }

    private void ValidateCreateLeaseRequest(RentalApplication application, Guid ownerId)
    {
        if (application.Status != ApplicationStatus.Approved)
        {
            throw new BadRequestException("Rental application is not approved.");
        }

        if (application.TenantVerificationRequests?.Any(tvr => tvr.Status == ApprovalStatus.Approved) != true)
        {
            throw new BadRequestException("Tenant verification request has not been approved yet.");
        }

        if (application.Property == null)
        {
            throw new NotFoundException("Property for application", application.PropertyId);
        }

        if (application.Property.OwnerId != ownerId)
        {
            throw new ForbiddenException("You do not own this property.");
        }
    }

    private async Task<string?> ProcessUnsignedAgreementAsync(Guid propertyId, Microsoft.AspNetCore.Http.IFormFile? file, CancellationToken ct)
    {
        if (file == null || file.Length == 0)
        {
            return null;
        }

        if (file.Length > 10 * 1024 * 1024)
        {
            throw new BadRequestException("File size exceeds 10MB limit.");
        }

        var allowedExtensions = new[] { ".pdf" };
        var extension = System.IO.Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!allowedExtensions.Contains(extension))
        {
            throw new BadRequestException("Only PDF files are allowed.");
        }

        return await _fileStorageService.SaveFileAsync(file, $"properties/{propertyId}/leases", ct);
    }

    private async Task<string?> ProcessSignedAgreementAsync(Guid propertyId, Microsoft.AspNetCore.Http.IFormFile? file, CancellationToken ct)
    {
        if (file == null || file.Length == 0)
        {
            return null;
        }

        if (file.Length > 10 * 1024 * 1024)
        {
            throw new BadRequestException("File size exceeds 10MB limit.");
        }

        var allowedExtensions = new[] { ".pdf", ".jpg", ".jpeg", ".png" };
        var extension = System.IO.Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!allowedExtensions.Contains(extension))
        {
            throw new BadRequestException("Only PDF, JPG, JPEG, and PNG files are allowed.");
        }

        return await _fileStorageService.SaveFileAsync(file, $"properties/{propertyId}/leases", ct);
    }

    private Lease CreateLeaseEntity(CreateLeaseRequest request, Guid propertyId, Guid tenantId, string? unsignedAgreementUrl)
    {
        return new Lease
        {
            LeaseId = Guid.NewGuid(),
            PropertyId = propertyId,
            TenantId = tenantId,
            ApplicationId = request.ApplicationId,
            StartDate = DateTime.SpecifyKind(request.StartDate, DateTimeKind.Utc),
            EndDate = DateTime.SpecifyKind(request.EndDate, DateTimeKind.Utc),
            MonthlyRent = request.MonthlyRent,
            SecurityDeposit = request.SecurityDeposit,
            PaymentFrequency = request.PaymentFrequency,
            NoticePeriodDays = request.NoticePeriodDays,
            PenaltyPercentage = request.PenaltyPercentage,
            AgreementText = request.AgreementText,
            UnsignedAgreementUrl = unsignedAgreementUrl,
            Status = LeaseStatus.PendingApproval,
            CreatedAt = DateTime.UtcNow
        };
    }

    private async Task CreateAndSendLeaseNotificationAsync(Guid tenantId, Guid leaseId, string propertyTitle)
    {
        var notification = new Notification
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
        };
        await _notificationRepo.AddAsync(notification);
    }

    private void UpdateLeaseSignature(Lease lease, string signatureText, bool isTenant, string? signedAgreementUrl)
    {
        if (isTenant)
        {
            lease.TenantSignature = signatureText;
            lease.TenantSignedAt = DateTime.UtcNow;
            if (signedAgreementUrl != null)
            {
                lease.SignedAgreementUrl = signedAgreementUrl;
            }
            _logger.LogInformation("Lease {LeaseId} signed by tenant {TenantId}", lease.LeaseId, lease.TenantId);
        }
        else
        {
            lease.OwnerSignature = signatureText;
            lease.OwnerSignedAt = DateTime.UtcNow;
            if (signedAgreementUrl != null)
            {
                lease.SignedAgreementUrl = signedAgreementUrl;
            }
            _logger.LogInformation("Lease {LeaseId} signed by owner {OwnerId}", lease.LeaseId, lease.Property?.OwnerId);
        }
    }

    public async Task<LeaseResponse> GetLeaseByIdAsync(Guid leaseId, Guid userId, bool isAdmin, CancellationToken ct)
    {
        var lease = await _leaseRepo.GetWithDetailsByIdAsync(leaseId);
        if (lease == null)
        {
            throw new NotFoundException("Lease", leaseId);
        }

        var property = lease.Property;
        bool isTenant = lease.TenantId == userId;
        bool isOwner = property != null && property.OwnerId == userId;

        if (!isTenant && !isOwner && !isAdmin)
        {
            throw new ForbiddenException("You are not authorized to view this lease.");
        }

        return _mapper.MapToResponse(lease);
    }

    public async Task<PagedResponse<LeaseResponse>> GetTenantLeasesAsync(Guid tenantId, int page, int limit, CancellationToken ct)
    {
        var paged = await _leaseRepo.GetByTenantIdPagedAsync(tenantId, page, limit);
        var mapped = paged.Items.Select(_mapper.MapToResponse);

        return new PagedResponse<LeaseResponse>
        {
            Success = true,
            Data = mapped,
            Page = page,
            Limit = limit,
            Total = paged.Total
        };
    }

    public async Task<PagedResponse<LeaseResponse>> GetOwnerLeasesAsync(Guid ownerId, int page, int limit, CancellationToken ct)
    {
        var paged = await _leaseRepo.GetByOwnerIdPagedAsync(ownerId, page, limit);
        var mapped = paged.Items.Select(_mapper.MapToResponse);

        return new PagedResponse<LeaseResponse>
        {
            Success = true,
            Data = mapped,
            Page = page,
            Limit = limit,
            Total = paged.Total
        };
    }

    public async Task<PagedResponse<LeaseResponse>> GetAllLeasesAsync(int page, int limit, CancellationToken ct)
    {
        var paged = await _leaseRepo.GetPagedAsync(page, limit);
        var mapped = paged.Items.Select(_mapper.MapToResponse);

        return new PagedResponse<LeaseResponse>
        {
            Success = true,
            Data = mapped,
            Page = page,
            Limit = limit,
            Total = paged.Total
        };
    }

    private async Task RejectOtherApplicationsOnLeaseActivationAsync(Guid propertyId, Guid currentApplicationId, string propertyTitle)
    {
        var applications = await _applicationRepo.GetByPropertyIdAsync(propertyId);
        foreach (var app in applications)
        {
            if (app.ApplicationId != currentApplicationId && 
                (app.Status == ApplicationStatus.Pending || app.Status == ApplicationStatus.Approved))
            {
                app.Status = ApplicationStatus.Rejected;
                app.ReviewRemarks = "Property is reserved";
                app.ReviewedAt = DateTime.UtcNow;

                var notification = new Notification
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
                };
                await _notificationRepo.AddAsync(notification);
            }
        }
    }
}
