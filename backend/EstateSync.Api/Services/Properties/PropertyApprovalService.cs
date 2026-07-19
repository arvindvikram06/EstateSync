using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using EstateSync.Api.Common;
using EstateSync.Api.DTOs.Properties;
using EstateSync.Api.Exceptions;
using EstateSync.Api.Mappers;
using EstateSync.Api.Models;
using EstateSync.Api.Repositories.Interfaces;
using EstateSync.Api.Interfaces.Services.Notifications;
using EstateSync.Api.Interfaces.Services.Properties;
using EstateSync.Api.Interfaces.Services.Shared;

namespace EstateSync.Api.Services.Properties;

public class PropertyApprovalService : IPropertyApprovalService
{
    private readonly IPropertyRepository _propertyRepo;
    private readonly IUserRepository _userRepo;
    private readonly INotificationSender _notificationSender;
    private readonly IUnitOfWork _unitOfWork;
    private readonly PropertyMapper _mapper;
    private readonly ILogger<PropertyApprovalService> _logger;
    private readonly IUrlResolverService _urlResolver;

    public PropertyApprovalService(
        IPropertyRepository propertyRepo,
        IUserRepository userRepo,
        INotificationSender notificationSender,
        IUnitOfWork unitOfWork,
        PropertyMapper mapper,
        ILogger<PropertyApprovalService> logger,
        IUrlResolverService urlResolver)
    {
        _propertyRepo = propertyRepo ?? throw new ArgumentNullException(nameof(propertyRepo));
        _userRepo = userRepo ?? throw new ArgumentNullException(nameof(userRepo));
        _notificationSender = notificationSender ?? throw new ArgumentNullException(nameof(notificationSender));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _urlResolver = urlResolver ?? throw new ArgumentNullException(nameof(urlResolver));
    }

    public async Task SubmitForApprovalAsync(Guid id, Guid ownerId, CancellationToken ct)
    {
        var property = await _propertyRepo.GetWithDetailsByIdAsync(id);
        if (property == null)
        {
            throw new NotFoundException("Property", id);
        }

        if (property.OwnerId != ownerId)
        {
            throw new ForbiddenException("You do not own this property.");
        }

        // Validate all required fields were filled in via the wizard before submission
        if (string.IsNullOrWhiteSpace(property.Title)       ||
            string.IsNullOrWhiteSpace(property.Description) ||
            string.IsNullOrWhiteSpace(property.State)       ||
            string.IsNullOrWhiteSpace(property.City)        ||
            string.IsNullOrWhiteSpace(property.Pincode)     ||
            string.IsNullOrWhiteSpace(property.AddressLine) ||
            property.MonthlyRent     <= 0  ||
            property.SecurityDeposit  < 0  ||
            property.Bedrooms        <= 0  ||
            property.Bathrooms       <= 0  ||
            property.LandArea        <= 0  ||
            property.BuildupArea     <= 0)
        {
            throw new BadRequestException("Please complete all required property details before submitting for approval.");
        }

        if (property.PropertyDocuments == null ||
            !property.PropertyDocuments.Any(pd => pd.Document != null && pd.Document.DocumentType == DocumentType.OwnershipCertificate))
        {
            throw new BadRequestException("You must upload a property ownership document before submitting for approval.");
        }

        if (property.PropertyImages == null || !property.PropertyImages.Any())
        {
            throw new BadRequestException("You must upload at least one property image before submitting for approval.");
        }

        if (property.Status != PropertyStatus.Draft && property.Status != PropertyStatus.Rejected)
        {
            throw new ConflictException("Property is not in a submittable state (Draft or Rejected).");
        }

        property.Status = PropertyStatus.PendingApproval;
        property.UpdatedAt = DateTime.UtcNow;

        await CreateAndSendAdminPropertyNotificationsAsync(property);

        await _unitOfWork.SaveChangesAsync(ct);
        _logger.LogInformation("Property {PropertyId} submitted for approval by owner {OwnerId}", id, ownerId);
    }

    public async Task ApproveOrRejectAsync(Guid id, Guid adminId, string status, string remarks, CancellationToken ct)
    {
        var property = await _propertyRepo.GetWithDetailsByIdAsync(id);
        if (property == null)
        {
            throw new NotFoundException("Property", id);
        }

        if (property.Status != PropertyStatus.PendingApproval)
        {
            throw new ConflictException("Property is not pending approval.");
        }
        
        if (!Enum.TryParse<PropertyStatus>(status, true, out var parsedStatus) ||
            (parsedStatus != PropertyStatus.Active && parsedStatus != PropertyStatus.Rejected))
        {
            throw new BadRequestException("Invalid approval status. Status must be Active or Rejected.");
        }

        property.Status = parsedStatus;
        property.ApproveById = adminId;
        property.ApproveRemarks = remarks;
        property.UpdatedAt = DateTime.UtcNow;

        if (property.PropertyDocuments != null)
        {
            foreach (var propDoc in property.PropertyDocuments)
            {
                if (propDoc.Document != null)
                {
                    propDoc.Document.ReviewStatus = parsedStatus == PropertyStatus.Active ? ApprovalStatus.Approved : ApprovalStatus.Rejected;
                    propDoc.Document.ReviewedAt = DateTime.UtcNow;
                    propDoc.Document.ReviewedById = adminId;
                    propDoc.Document.ReviewNotes = remarks;
                }
            }
        }

        await CreateAndSendOwnerPropertyNotificationAsync(property, parsedStatus, remarks);

        await _unitOfWork.SaveChangesAsync(ct);
        _logger.LogInformation("Property {PropertyId} approval status updated to {Status} by admin {AdminId}", id, parsedStatus, adminId);
    }

    public async Task<PagedResponse<PropertySummaryResponse>> GetPendingApprovalPropertiesAsync(int page, int limit, CancellationToken ct)
    {
        var offset = (page - 1) * limit;
        var (items, total) = await _propertyRepo.GetPendingApprovalPropertiesAsync(limit, offset);

        var resolved = new System.Collections.Generic.List<PropertySummaryResponse>();
        foreach (var item in items)
            resolved.Add(await _urlResolver.ResolveAsync(_mapper.MapToSummaryResponse(item), ct));

        return new PagedResponse<PropertySummaryResponse>
        {
            Success = true,
            Data = resolved,
            Page = page,
            Limit = limit,
            Total = total
        };
    }

    public async Task<PagedResponse<PropertySummaryResponse>> GetAllAdminPropertiesAsync(string? search, string? status, int page, int limit, CancellationToken ct)
    {
        var offset = (page - 1) * limit;
        var (items, total) = await _propertyRepo.GetAllAdminAsync(search, status, limit, offset);

        var resolved = new System.Collections.Generic.List<PropertySummaryResponse>();
        foreach (var item in items)
            resolved.Add(await _urlResolver.ResolveAsync(_mapper.MapToSummaryResponse(item), ct));

        return new PagedResponse<PropertySummaryResponse>
        {
            Success = true,
            Data = resolved,
            Page = page,
            Limit = limit,
            Total = total
        };
    }

    private async Task CreateAndSendAdminPropertyNotificationsAsync(Property property)
    {
        var admins = await _userRepo.GetUsersByRoleAsync(Roles.Admin);
        foreach (var admin in admins)
        {
            var notification = new Notification
            {
                NotificationId = Guid.NewGuid(),
                UserId = admin.UserId,
                Type = NotificationType.General,
                Title = "Property Verification Pending",
                Message = $"Property '{property.Title}' is pending approval.",
                EntityId = property.PropertyId.ToString(),
                EntityType = "Property",
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            };
            await _notificationSender.SendAsync(notification);
        }
    }

    private async Task CreateAndSendOwnerPropertyNotificationAsync(Property property, PropertyStatus status, string remarks)
    {
        var title = status == PropertyStatus.Active ? "Property Approved" : "Property Rejected";
        var message = status == PropertyStatus.Active
            ? $"Your property '{property.Title}' has been approved and is now active."
            : $"Your property '{property.Title}' has been rejected. Reason: {remarks}";

        var notification = new Notification
        {
            NotificationId = Guid.NewGuid(),
            UserId = property.OwnerId,
            Type = NotificationType.General,
            Title = title,
            Message = message,
            EntityId = property.PropertyId.ToString(),
            EntityType = "Property",
            IsRead = false,
            CreatedAt = DateTime.UtcNow
        };
        await _notificationSender.SendAsync(notification);
    }
}
