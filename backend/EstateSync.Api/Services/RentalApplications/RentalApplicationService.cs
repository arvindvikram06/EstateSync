using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using EstateSync.Api.Common;
using EstateSync.Api.DTOs.RentalApplications;
using EstateSync.Api.Exceptions;
using EstateSync.Api.Interfaces.Services.RentalApplications;
using EstateSync.Api.Interfaces.Services.Auth;
using EstateSync.Api.Interfaces.Services.Notifications;
using EstateSync.Api.Mappers;
using EstateSync.Api.Models;
using EstateSync.Api.Repositories.Interfaces;

namespace EstateSync.Api.Services.RentalApplications;

public class RentalApplicationService : IRentalApplicationService
{
    private readonly IRentalApplicationRepository _rentalAppRepo;
    private readonly IPropertyRepository _propertyRepo;
    private readonly IUserRepository _userRepo;
    private readonly ILeaseRepository _leaseRepo;
    private readonly INotificationSender _notificationSender;
    private readonly IUnitOfWork _unitOfWork;
    private readonly RentalApplicationMapper _mapper;
    private readonly ILogger<RentalApplicationService> _logger;

    public RentalApplicationService(
        IRentalApplicationRepository rentalAppRepo,
        IPropertyRepository propertyRepo,
        IUserRepository userRepo,
        ILeaseRepository leaseRepo,
        INotificationSender notificationSender,
        IUnitOfWork unitOfWork,
        RentalApplicationMapper mapper,
        ILogger<RentalApplicationService> logger)
    {
        _rentalAppRepo = rentalAppRepo ?? throw new ArgumentNullException(nameof(rentalAppRepo));
        _propertyRepo = propertyRepo ?? throw new ArgumentNullException(nameof(propertyRepo));
        _userRepo = userRepo ?? throw new ArgumentNullException(nameof(userRepo));
        _leaseRepo = leaseRepo ?? throw new ArgumentNullException(nameof(leaseRepo));
        _notificationSender = notificationSender ?? throw new ArgumentNullException(nameof(notificationSender));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<RentalApplicationResponse> CreateApplicationAsync(
        Guid propertyId,
        CreateRentalApplicationRequest request,
        Guid tenantId,
        CancellationToken ct)
    {
        var tenant = await _userRepo.GetByIdAsync(tenantId);
        if (tenant == null) throw new NotFoundException("User", tenantId);

        var property = await _propertyRepo.GetByIdAsync(propertyId);
        if (property == null)
        {
            throw new NotFoundException("Property", propertyId);
        }

        await ValidateCreateApplicationAsync(propertyId, tenant, property, tenantId, request.ExpectedMoveInDate);

        var application = CreateApplicationEntity(propertyId, tenantId, request);
        await _rentalAppRepo.AddAsync(application);

        await SendApplicationReceivedNotificationAsync(property.OwnerId, application.ApplicationId, property.Title);
        await _unitOfWork.SaveChangesAsync(ct);

        _logger.LogInformation("Rental application {ApplicationId} created for property {PropertyId} by tenant {TenantId}",
            application.ApplicationId, propertyId, tenantId);

        application.Property = property;
        application.Tenant = tenant;

        return _mapper.MapToResponse(application);
    }

    public async Task<RentalApplicationResponse> ReviewApplicationAsync(
        Guid applicationId,
        bool approve,
        string remarks,
        Guid ownerId,
        CancellationToken ct)
    {
        var application = await _rentalAppRepo.GetWithDetailsByIdAsync(applicationId);
        if (application == null)
        {
            throw new NotFoundException("RentalApplication", applicationId);
        }

        ValidateReviewApplication(application, ownerId);

        if (approve)
        {
            await ProcessApprovalAsync(application, remarks, ct);
        }
        else
        {
            await ProcessRejectionAsync(application, remarks);
        }

        await _unitOfWork.SaveChangesAsync(ct);

        _logger.LogInformation("Rental application {ApplicationId} reviewed. Approved: {Approved} by owner {OwnerId}",
            applicationId, approve, ownerId);

        return _mapper.MapToResponse(application);
    }

    public async Task<PagedResponse<RentalApplicationResponse>> GetOwnerApplicationsAsync(Guid ownerId, int page, int limit, CancellationToken ct)
    {
        var (apps, total) = await _rentalAppRepo.GetByOwnerIdPagedAsync(ownerId, page, limit);
        var mapped = apps.Select(_mapper.MapToResponse);
        return new PagedResponse<RentalApplicationResponse>
        {
            Success = true,
            Data = mapped,
            Page = page,
            Limit = limit,
            Total = total
        };
    }

    public async Task<PagedResponse<RentalApplicationResponse>> GetApplicationsByPropertyIdAsync(
        Guid propertyId,
        Guid ownerId,
        int page,
        int limit,
        CancellationToken ct)
    {
        var property = await _propertyRepo.GetByIdAsync(propertyId);
        if (property == null)
        {
            throw new NotFoundException("Property", propertyId);
        }

        if (property.OwnerId != ownerId)
        {
            throw new ForbiddenException("You do not own this property.");
        }

        var (apps, total) = await _rentalAppRepo.GetByPropertyIdWithDetailsPagedAsync(propertyId, page, limit);
        var mapped = apps.Select(_mapper.MapToResponse);
        return new PagedResponse<RentalApplicationResponse>
        {
            Success = true,
            Data = mapped,
            Page = page,
            Limit = limit,
            Total = total
        };
    }

    public async Task<PagedResponse<RentalApplicationResponse>> GetTenantApplicationsAsync(Guid tenantId, int page, int limit, CancellationToken ct)
    {
        var (apps, total) = await _rentalAppRepo.GetByTenantIdWithDetailsPagedAsync(tenantId, page, limit);
        var mapped = apps.Select(_mapper.MapToResponse);
        return new PagedResponse<RentalApplicationResponse>
        {
            Success = true,
            Data = mapped,
            Page = page,
            Limit = limit,
            Total = total
        };
    }


  
    public async Task<RentalApplicationResponse> GetApplicationByIdAsync(
        Guid applicationId,
        Guid userId,
        bool isAdmin,
        CancellationToken ct)
    {
        var application = await _rentalAppRepo.GetWithDetailsByIdAsync(applicationId);
        if (application == null)
        {
            throw new NotFoundException("RentalApplication", applicationId);
        }

        if (isAdmin)
        {
            // Admin has full access
        }
        else if (application.TenantId == userId)
        {
            // Caller is the tenant who submitted the application
        }
        else if (application.Property != null && application.Property.OwnerId == userId)
        {
            // Caller is the owner of the property
        }
        else
        {
            throw new ForbiddenException("You are not authorized to view this application.");
        }

        return _mapper.MapToResponse(application);
    }


    
    private async Task ValidateCreateApplicationAsync(Guid propertyId, User tenant, Property property, Guid tenantId, DateOnly moveInDate)
    {
        if (!tenant.IsKycVerified)
        {
            throw new ForbiddenException("You must complete KYC verification before applying for a rental property.");
        }

        if (property.RequiresVerifiedIncome && !tenant.IsFinanciallyVerified)
        {
            throw new ForbiddenException("This property requires a verified income profile. Please submit your financial profile first.");
        }

        if (property.RequiresVerifiedIncome && tenant.IsFinanciallyVerified
            && tenant.VerifiedMonthlyIncome < property.MonthlyRent * 3)
        {
            _logger.LogWarning("Tenant {TenantId} has insufficient verified income (₹{VerifiedIncome}/month) for property {PropertyId} with rent ₹{MonthlyRent}/month",
                tenantId, tenant.VerifiedMonthlyIncome, propertyId, property.MonthlyRent);
            throw new ForbiddenException($"Your verified income (₹{tenant.VerifiedMonthlyIncome:N0}/month) does not meet the minimum requirement of 3× monthly rent (₹{property.MonthlyRent * 3:N0}) for this property.");
        }

        if (property.Status != PropertyStatus.Active && property.Status != PropertyStatus.AvailableSoon)
        {
            throw new BadRequestException("You can only apply to properties that are Active or Available Soon.");
        }

        if (property.OwnerId == tenantId)
        {
            throw new BadRequestException("You cannot apply to your own property.");
        }

        var (propertyActiveLease, tenantActiveLease) = await _leaseRepo.GetActiveLeaseConflictsAsync(propertyId, tenantId);
        if (propertyActiveLease != null && moveInDate <= DateOnly.FromDateTime(propertyActiveLease.EndDate))
        {
            throw new ConflictException("The move-in date must be after the current active lease ends.");
        }

        if (tenantActiveLease != null && moveInDate <= DateOnly.FromDateTime(tenantActiveLease.EndDate))
        {
            throw new ConflictException("You currently have an active lease. Your move-in date must be after your current lease ends.");
        }

        var alreadyApplied = await _rentalAppRepo.HasTenantAppliedAsync(propertyId, tenantId);
        if (alreadyApplied)
        {
            throw new ConflictException("You have already applied to this property.");
        }
    }

    private RentalApplication CreateApplicationEntity(Guid propertyId, Guid tenantId, CreateRentalApplicationRequest request)
    {
        return new RentalApplication
        {
            ApplicationId = Guid.NewGuid(),
            PropertyId = propertyId,
            TenantId = tenantId,
            Message = request.Message,
            ExpectedMoveInDate = request.ExpectedMoveInDate,
            DurationMonths = request.DurationMonths,
            CommuteDistance = request.CommuteDistance,
            Status = ApplicationStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };
    }

    private async Task SendApplicationReceivedNotificationAsync(Guid ownerId, Guid applicationId, string propertyTitle)
    {
        var notification = new Notification
        {
            NotificationId = Guid.NewGuid(),
            UserId = ownerId,
            Type = NotificationType.ApplicationReceived,
            Title = "New Rental Application",
            Message = $"You have received a new rental application for property '{propertyTitle}'.",
            EntityId = applicationId.ToString(),
            EntityType = "RentalApplication",
            IsRead = false,
            CreatedAt = DateTime.UtcNow
        };
        await _notificationSender.SendAsync(notification);
    }

    private void ValidateReviewApplication(RentalApplication application, Guid ownerId)
    {
        if (application.Property == null || application.Property.OwnerId != ownerId)
        {
            throw new ForbiddenException("You do not own the property for this application.");
        }

        if (application.Status != ApplicationStatus.Pending)
        {
            throw new BadRequestException("This application has already been processed.");
        }
    }

    private async Task ProcessApprovalAsync(RentalApplication application, string remarks, CancellationToken ct)
    {
        application.Status = ApplicationStatus.Approved;
        application.ReviewRemarks = remarks;
        application.ReviewedAt = DateTime.UtcNow;

        var approveNotification = new Notification
        {
            NotificationId = Guid.NewGuid(),
            UserId = application.TenantId,
            Type = NotificationType.ApplicationApproved,
            Title = "Application Approved",
            Message = $"Your application for property '{application.Property!.Title}' has been approved by the owner!",
            EntityId = application.ApplicationId.ToString(),
            EntityType = "RentalApplication",
            IsRead = false,
            CreatedAt = DateTime.UtcNow
        };
        await _notificationSender.SendAsync(approveNotification);
    }

    private async Task ProcessRejectionAsync(RentalApplication application, string remarks)
    {
        application.Status = ApplicationStatus.Rejected;
        application.ReviewRemarks = remarks;
        application.ReviewedAt = DateTime.UtcNow;


        var rejectNotification = new Notification
        {
            NotificationId = Guid.NewGuid(),
            UserId = application.TenantId,
            Type = NotificationType.ApplicationRejected,
            Title = "Application Rejected",
            Message = $"Your application for property '{application.Property!.Title}' was rejected by the owner.",
            EntityId = application.ApplicationId.ToString(),
            EntityType = "RentalApplication",
            IsRead = false,
            CreatedAt = DateTime.UtcNow
        };
        await _notificationSender.SendAsync(rejectNotification);
    }
}
