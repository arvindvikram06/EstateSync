using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EstateSync.Api.Common;
using EstateSync.Api.DTOs.SiteVisits;
using EstateSync.Api.Exceptions;
using EstateSync.Api.Interfaces.Services.Notifications;
using EstateSync.Api.Interfaces.Services.SiteVisits;
using EstateSync.Api.Mappers;
using EstateSync.Api.Models;
using EstateSync.Api.Repositories.Interfaces;

namespace EstateSync.Api.Services.SiteVisits;

public class SiteVisitService : ISiteVisitService
{
    private readonly ISiteVisitRepository _siteVisitRepo;
    private readonly IPropertyRepository _propertyRepo;
    private readonly INotificationSender _notificationSender;
    private readonly IUnitOfWork _unitOfWork;
    private readonly SiteVisitMapper _mapper;

    public SiteVisitService(
        ISiteVisitRepository siteVisitRepo,
        IPropertyRepository propertyRepo,
        INotificationSender notificationSender,
        IUnitOfWork unitOfWork,
        SiteVisitMapper mapper)
    {
        _siteVisitRepo = siteVisitRepo ?? throw new ArgumentNullException(nameof(siteVisitRepo));
        _propertyRepo = propertyRepo ?? throw new ArgumentNullException(nameof(propertyRepo));
        _notificationSender = notificationSender ?? throw new ArgumentNullException(nameof(notificationSender));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
    }

    public async Task<SiteVisitResponse> RequestVisitAsync(Guid tenantId, CreateSiteVisitRequest request, CancellationToken ct = default)
    {
        var property = await _propertyRepo.GetByIdAsync(request.PropertyId);
        if (property == null)
        {
            throw new NotFoundException("Property", request.PropertyId);
        }

        if (property.OwnerId == tenantId)
        {
            throw new ForbiddenException("Owners cannot book site visits on their own properties.");
        }

        var hasActive = await _siteVisitRepo.HasActiveRequestAsync(tenantId, request.PropertyId);
        if (hasActive)
        {
            throw new ConflictException("You already have an active site visit request for this property.");
        }

        var visit = new SiteVisit
        {
            SiteVisitId = Guid.NewGuid(),
            PropertyId = request.PropertyId,
            TenantId = tenantId,
            VisitDate = request.VisitDate.Date,
            VisitTime = request.VisitTime,
            Status = SiteVisitStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };

        await _siteVisitRepo.AddAsync(visit);

        await _notificationSender.SendAsync(new Notification
        {
            NotificationId = Guid.NewGuid(),
            UserId = property.OwnerId,
            Type = NotificationType.SiteVisitRequested,
            Title = "New Site Visit Requested",
            Message = $"A new site visit has been requested for '{property.Title}' on {request.VisitDate:yyyy-MM-dd} at {request.VisitTime:HH:mm}.",
            EntityId = visit.SiteVisitId.ToString(),
            EntityType = "SiteVisit",
            IsRead = false,
            CreatedAt = DateTime.UtcNow
        }, ct);

        await _unitOfWork.SaveChangesAsync(ct);

        var savedVisit = await _siteVisitRepo.GetWithDetailsByIdAsync(visit.SiteVisitId);
        return _mapper.MapToResponse(savedVisit ?? visit);
    }

    public async Task<SiteVisitResponse> UpdateStatusAsync(Guid userId, Guid siteVisitId, UpdateSiteVisitStatusRequest request, CancellationToken ct = default)
    {
        var visit = await _siteVisitRepo.GetWithDetailsByIdAsync(siteVisitId);
        if (visit == null)
        {
            throw new NotFoundException("SiteVisit", siteVisitId);
        }

        if (request.Status == SiteVisitStatus.Approved || request.Status == SiteVisitStatus.Declined)
        {
            if (visit.Property == null || visit.Property.OwnerId != userId)
            {
                throw new ForbiddenException("Only the property owner can approve or decline a site visit.");
            }

            if (visit.Status != SiteVisitStatus.Pending)
            {
                throw new BadRequestException("Only pending site visits can be approved or declined.");
            }

            visit.Status = request.Status;
            visit.OwnerNotes = request.OwnerNotes;

            if (request.Status == SiteVisitStatus.Approved)
            {
                await _notificationSender.SendAsync(new Notification
                {
                    NotificationId = Guid.NewGuid(),
                    UserId = visit.TenantId,
                    Type = NotificationType.SiteVisitApproved,
                    Title = "Site Visit Confirmed",
                    Message = $"Your site visit for '{visit.Property?.Title}' has been confirmed for {visit.VisitDate:yyyy-MM-dd} at {visit.VisitTime:HH:mm}.",
                    EntityId = visit.SiteVisitId.ToString(),
                    EntityType = "SiteVisit",
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow
                }, ct);
            }
            else
            {
                await _notificationSender.SendAsync(new Notification
                {
                    NotificationId = Guid.NewGuid(),
                    UserId = visit.TenantId,
                    Type = NotificationType.SiteVisitDeclined,
                    Title = "Site Visit Declined",
                    Message = $"Your site visit for '{visit.Property?.Title}' was declined. Note: {request.OwnerNotes}",
                    EntityId = visit.SiteVisitId.ToString(),
                    EntityType = "SiteVisit",
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow
                }, ct);
            }
        }
        else if (request.Status == SiteVisitStatus.Cancelled)
        {
            if (visit.TenantId != userId)
            {
                throw new ForbiddenException("Only the tenant who requested the visit can cancel it.");
            }

            if (visit.Status == SiteVisitStatus.Cancelled || visit.Status == SiteVisitStatus.Declined)
            {
                throw new BadRequestException("This site visit cannot be cancelled in its current state.");
            }

            visit.Status = SiteVisitStatus.Cancelled;

            if (visit.Property != null)
            {
                await _notificationSender.SendAsync(new Notification
                {
                    NotificationId = Guid.NewGuid(),
                    UserId = visit.Property.OwnerId,
                    Type = NotificationType.SiteVisitCancelled,
                    Title = "Site Visit Cancelled",
                    Message = $"The site visit for '{visit.Property?.Title}' has been cancelled by the tenant.",
                    EntityId = visit.SiteVisitId.ToString(),
                    EntityType = "SiteVisit",
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow
                }, ct);
            }
        }
        else if (request.Status == SiteVisitStatus.Completed)
        {
            if (visit.Property == null || visit.Property.OwnerId != userId)
                throw new ForbiddenException("Only the property owner can mark a visit as completed.");

            if (visit.Status != SiteVisitStatus.Approved)
                throw new BadRequestException("Only approved visits can be marked as completed.");

            visit.Status = SiteVisitStatus.Completed;
        }
        else
        {
            throw new BadRequestException("Invalid status transition requested.");
        }

        await _unitOfWork.SaveChangesAsync(ct);
        return _mapper.MapToResponse(visit);
    }

    public async Task<IEnumerable<SiteVisitResponse>> GetTenantVisitsAsync(Guid tenantId, CancellationToken ct = default)
    {
        var visits = await _siteVisitRepo.GetByTenantIdAsync(tenantId);
        return visits.Select(_mapper.MapToResponse);
    }

    public async Task<IEnumerable<SiteVisitResponse>> GetOwnerVisitsAsync(Guid ownerId, CancellationToken ct = default)
    {
        var visits = await _siteVisitRepo.GetByOwnerIdAsync(ownerId);
        return visits.Select(_mapper.MapToResponse);
    }

    public async Task<SiteVisitResponse> GetByIdAsync(Guid id, Guid userId, CancellationToken ct = default)
    {
        var visit = await _siteVisitRepo.GetWithDetailsByIdAsync(id);
        if (visit == null)
        {
            throw new NotFoundException("SiteVisit", id);
        }

        if (visit.TenantId != userId && (visit.Property == null || visit.Property.OwnerId != userId))
        {
            throw new ForbiddenException("You are not authorized to view this site visit.");
        }

        return _mapper.MapToResponse(visit);
    }
}
