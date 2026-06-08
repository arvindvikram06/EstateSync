using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using EstateSync.Api.Common;
using EstateSync.Api.DTOs.RentalApplications;
using EstateSync.Api.Exceptions;
using EstateSync.Api.Interfaces.Services.RentalApplications;
using EstateSync.Api.Mappers;
using EstateSync.Api.Models;
using EstateSync.Api.Repositories.Interfaces;

namespace EstateSync.Api.Services.RentalApplications;

public class RentalApplicationService : IRentalApplicationService
{
    private readonly IRentalApplicationRepository _rentalAppRepo;
    private readonly IPropertyRepository _propertyRepo;
    private readonly IUserRepository _userRepo;
    private readonly INotificationRepository _notificationRepo;
    private readonly IUnitOfWork _unitOfWork;
    private readonly RentalApplicationMapper _mapper;
    private readonly ILogger<RentalApplicationService> _logger;

    public RentalApplicationService(
        IRentalApplicationRepository rentalAppRepo,
        IPropertyRepository propertyRepo,
        IUserRepository userRepo,
        INotificationRepository notificationRepo,
        IUnitOfWork unitOfWork,
        RentalApplicationMapper mapper,
        ILogger<RentalApplicationService> logger)
    {
        _rentalAppRepo = rentalAppRepo ?? throw new ArgumentNullException(nameof(rentalAppRepo));
        _propertyRepo = propertyRepo ?? throw new ArgumentNullException(nameof(propertyRepo));
        _userRepo = userRepo ?? throw new ArgumentNullException(nameof(userRepo));
        _notificationRepo = notificationRepo ?? throw new ArgumentNullException(nameof(notificationRepo));
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
        var property = await _propertyRepo.GetByIdAsync(propertyId);
        if (property == null)
        {
            throw new NotFoundException("Property", propertyId);
        }

        if (property.Status != PropertyStatus.Active)
        {
            throw new BadRequestException("You can only apply to Active properties.");
        }

        if (property.OwnerId == tenantId)
        {
            throw new BadRequestException("You cannot apply to your own property.");
        }

        var alreadyApplied = await _rentalAppRepo.HasTenantAppliedAsync(propertyId, tenantId);
        if (alreadyApplied)
        {
            throw new ConflictException("You have already applied to this property.");
        }

        var tenant = await _userRepo.GetByIdAsync(tenantId);
        if (tenant == null)
        {
            throw new NotFoundException("User", tenantId);
        }

        var application = new RentalApplication
        {
            ApplicationId = Guid.NewGuid(),
            PropertyId = propertyId,
            TenantId = tenantId,
            Message = request.Message,
            DocumentCount = 0,
            MoveInDate = request.MoveInDate,
            MoveOutDate = request.MoveOutDate,
            NegotiatedPrice = request.NegotiatedPrice,
            CommuteDistance = request.CommuteDistance,
            Status = ApplicationStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };

        await _rentalAppRepo.AddAsync(application);

        var notification = new Notification
        {
            NotificationId = Guid.NewGuid(),
            UserId = property.OwnerId,
            Type = NotificationType.ApplicationReceived,
            Title = "New Rental Application",
            Message = $"You have received a new rental application for property '{property.Title}'.",
            EntityId = application.ApplicationId.ToString(),
            EntityType = "RentalApplication",
            IsRead = false,
            CreatedAt = DateTime.UtcNow
        };

        await _notificationRepo.AddAsync(notification);

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

        if (application.Property == null || application.Property.OwnerId != ownerId)
        {
            throw new ForbiddenException("You do not own the property for this application.");
        }

        if (application.Status != ApplicationStatus.Pending)
        {
            throw new BadRequestException("This application has already been processed.");
        }

        if (approve)
        {
            application.Status = ApplicationStatus.Approved;
            application.ReviewRemarks = remarks;
            application.ReviewedAt = DateTime.UtcNow;

            await _rentalAppRepo.UpdateAsync(application);

            // Automatically reject all other pending applications for this property
            var allAppsForProperty = await _rentalAppRepo.GetByPropertyIdAsync(application.PropertyId);
            foreach (var otherApp in allAppsForProperty)
            {
                if (otherApp.ApplicationId != application.ApplicationId && otherApp.Status == ApplicationStatus.Pending)
                {
                    otherApp.Status = ApplicationStatus.Rejected;
                    otherApp.ReviewRemarks = "Another application was approved for this property.";
                    otherApp.ReviewedAt = DateTime.UtcNow;

                    await _rentalAppRepo.UpdateAsync(otherApp);

                    var rejectNotification = new Notification
                    {
                        NotificationId = Guid.NewGuid(),
                        UserId = otherApp.TenantId,
                        Type = NotificationType.ApplicationRejected,
                        Title = "Application Rejected",
                        Message = $"Your application for property '{application.Property.Title}' was rejected.",
                        EntityId = otherApp.ApplicationId.ToString(),
                        EntityType = "RentalApplication",
                        IsRead = false,
                        CreatedAt = DateTime.UtcNow
                    };
                    await _notificationRepo.AddAsync(rejectNotification);
                }
            }

            // Send approval notification to tenant
            var approveNotification = new Notification
            {
                NotificationId = Guid.NewGuid(),
                UserId = application.TenantId,
                Type = NotificationType.ApplicationApproved,
                Title = "Application Approved",
                Message = $"Your application for property '{application.Property.Title}' has been approved by the owner!",
                EntityId = application.ApplicationId.ToString(),
                EntityType = "RentalApplication",
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            };
            await _notificationRepo.AddAsync(approveNotification);
        }
        else
        {
            application.Status = ApplicationStatus.Rejected;
            application.ReviewRemarks = remarks;
            application.ReviewedAt = DateTime.UtcNow;

            await _rentalAppRepo.UpdateAsync(application);

            // Send rejection notification to tenant
            var rejectNotification = new Notification
            {
                NotificationId = Guid.NewGuid(),
                UserId = application.TenantId,
                Type = NotificationType.ApplicationRejected,
                Title = "Application Rejected",
                Message = $"Your application for property '{application.Property.Title}' was rejected by the owner.",
                EntityId = application.ApplicationId.ToString(),
                EntityType = "RentalApplication",
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            };
            await _notificationRepo.AddAsync(rejectNotification);
        }

        await _unitOfWork.SaveChangesAsync(ct);

        _logger.LogInformation("Rental application {ApplicationId} reviewed. Approved: {Approved} by owner {OwnerId}",
            applicationId, approve, ownerId);

        return _mapper.MapToResponse(application);
    }

    public async Task<IEnumerable<RentalApplicationResponse>> GetOwnerApplicationsAsync(Guid ownerId, CancellationToken ct)
    {
        var apps = await _rentalAppRepo.GetByOwnerIdAsync(ownerId);
        return System.Linq.Enumerable.Select(apps, _mapper.MapToResponse);
    }

    public async Task<IEnumerable<RentalApplicationResponse>> GetApplicationsByPropertyIdAsync(
        Guid propertyId,
        Guid ownerId,
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

        var apps = await _rentalAppRepo.GetByPropertyIdWithDetailsAsync(propertyId);
        return System.Linq.Enumerable.Select(apps, _mapper.MapToResponse);
    }
}
