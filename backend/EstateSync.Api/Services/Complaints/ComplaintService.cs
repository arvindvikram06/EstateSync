using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using EstateSync.Api.Common;
using EstateSync.Api.Data;
using EstateSync.Api.DTOs.Complaints;
using EstateSync.Api.Exceptions;
using EstateSync.Api.Interfaces.Services.Complaints;
using EstateSync.Api.Interfaces.Services.Notifications;
using EstateSync.Api.Interfaces.Services.Storage;
using EstateSync.Api.Interfaces.Services.Shared;
using EstateSync.Api.Mappers;
using EstateSync.Api.Models;
using EstateSync.Api.Repositories.Interfaces;

namespace EstateSync.Api.Services.Complaints;

public class ComplaintService : IComplaintService
{
    private readonly IComplaintRepository _complaintRepo;
    private readonly AppDbContext _db;
    private readonly ILeaseRepository _leaseRepo;
    private readonly IUserRepository _userRepo;
    private readonly IFileStorageService _storageService;
    private readonly INotificationSender _notificationSender;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ComplaintMapper _mapper;
    private readonly IUrlResolverService _urlResolver;

    public ComplaintService(
        IComplaintRepository complaintRepo,
        AppDbContext db,
        ILeaseRepository leaseRepo,
        IUserRepository userRepo,
        IFileStorageService storageService,
        INotificationSender notificationSender,
        IUnitOfWork unitOfWork,
        ComplaintMapper mapper,
        IUrlResolverService urlResolver)
    {
        _complaintRepo = complaintRepo ?? throw new ArgumentNullException(nameof(complaintRepo));
        _db = db ?? throw new ArgumentNullException(nameof(db));
        _leaseRepo = leaseRepo ?? throw new ArgumentNullException(nameof(leaseRepo));
        _userRepo = userRepo ?? throw new ArgumentNullException(nameof(userRepo));
        _storageService = storageService ?? throw new ArgumentNullException(nameof(storageService));
        _notificationSender = notificationSender ?? throw new ArgumentNullException(nameof(notificationSender));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        _urlResolver = urlResolver ?? throw new ArgumentNullException(nameof(urlResolver));
    }

    public async Task<ComplaintResponse> CreateComplaintAsync(Guid tenantId, CreateComplaintRequest request, CancellationToken ct = default)
    {
        var lease = await _leaseRepo.GetWithDetailsByIdAsync(request.LeaseId);
        if (lease == null)
        {
            throw new NotFoundException("Lease", request.LeaseId);
        }

        if (lease.TenantId != tenantId)
        {
            throw new ForbiddenException("You are not authorized to file a complaint for this lease.");
        }

        var complaint = new Complaint
        {
            ComplaintId = Guid.NewGuid(),
            LeaseId = request.LeaseId,
            LeaseBillId = request.LeaseBillId,
            Title = request.Title,
            Description = request.Description,
            ComplaintType = request.ComplaintType,
            Priority = request.Priority,
            Status = ComplaintStatus.Open,
            CreatedAt = DateTime.UtcNow
        };
        var tenant = await _userRepo.GetByIdAsync(tenantId);
        complaint.Activities.Add(CreateActivity(
            complaint.ComplaintId, tenantId, tenant?.FullName ?? "Tenant", Roles.Tenant,
            "Created", null, ComplaintStatus.Open, "Complaint submitted"));

        await _complaintRepo.AddAsync(complaint);

        if (lease.Property != null)
        {
            await _notificationSender.SendAsync(new Notification
            {
                NotificationId = Guid.NewGuid(),
                UserId = lease.Property.OwnerId,
                Type = NotificationType.ComplaintRaised,
                Title = "New Complaint Filed",
                Message = $"A new complaint has been raised for '{lease.Property.Title}': {request.Title}",
                EntityId = complaint.ComplaintId.ToString(),
                EntityType = "Complaint",
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            });
        }

        await _unitOfWork.SaveChangesAsync(ct);

        // Fetch back with details to ensure clean output mapping
        var savedComplaint = await _complaintRepo.GetWithDetailsByIdAsync(complaint.ComplaintId);
        return _mapper.MapToResponse(savedComplaint ?? complaint);
    }

    public async Task<ComplaintResponse> UpdateComplaintStatusAsync(Guid userId, Guid complaintId, UpdateComplaintStatusRequest request, CancellationToken ct = default)
    {
        var complaint = await _complaintRepo.GetWithDetailsByIdAsync(complaintId);
        if (complaint == null)
        {
            throw new NotFoundException("Complaint", complaintId);
        }

        var user = await _userRepo.GetByIdWithRolesAsync(userId);
        if (user == null)
        {
            throw new NotFoundException("User", userId);
        }

        var isAdmin = user.UserRoles.Any(ur => ur.Role != null && ur.Role.Name == Roles.Admin);
        var isTenant = complaint.Lease?.TenantId == userId;
        var isOwner = complaint.Lease?.Property != null && complaint.Lease.Property.OwnerId == userId;

        if (isAdmin)
            throw new ForbiddenException("Admins can review complaints but cannot change their workflow status.");

        if (!isAdmin && !isTenant && !isOwner)
            throw new ForbiddenException("You are not authorized to update this complaint.");

        var previous = complaint.Status;
        var remarks = request.ReviewRemarks?.Trim();
        ValidateTransition(previous, request.Status, isOwner, isTenant, isAdmin, remarks);

        complaint.Status = request.Status;
        complaint.ReviewRemarks = remarks;
        complaint.UpdatedAt = DateTime.UtcNow;
        var activityType = "StatusChanged";

        if (request.Status == ComplaintStatus.ResolutionProposed)
        {
            complaint.ResolutionSummary = remarks;
            complaint.ResolutionProposedAt = DateTime.UtcNow;
            activityType = "ResolutionProposed";
        }
        else if (request.Status == ComplaintStatus.Reopened)
        {
            complaint.ReopenReason = remarks;
            complaint.ReopenCount++;
            complaint.LastReopenedAt = DateTime.UtcNow;
            complaint.ResolvedAt = null;
            activityType = "Reopened";
        }
        else if (request.Status == ComplaintStatus.Resolved)
        {
            complaint.ResolvedAt = DateTime.UtcNow;
            activityType = "ResolutionConfirmed";
        }

        var actorRole = isOwner ? Roles.Owner : isTenant ? Roles.Tenant : Roles.Admin;
        var activity = CreateActivity(
            complaint.ComplaintId, userId, user.FullName, actorRole,
            activityType, previous, request.Status, remarks);
        activity.Complaint = complaint;
        await _db.ComplaintActivities.AddAsync(activity, ct);

        var recipientId = isOwner ? complaint.Lease?.TenantId : complaint.Lease?.Property?.OwnerId;
        if (recipientId.HasValue)
            await SendStatusNotificationAsync(recipientId.Value, complaint, request.Status, ct);

        await _unitOfWork.SaveChangesAsync(ct);

        return _mapper.MapToResponse(complaint);
    }

    private static void ValidateTransition(
        ComplaintStatus current,
        ComplaintStatus next,
        bool isOwner,
        bool isTenant,
        bool isAdmin,
        string? remarks)
    {
        if (isAdmin) return;

        var allowed = isOwner
            ? (current, next) switch
            {
                (ComplaintStatus.Open, ComplaintStatus.InProgress) => true,
                (ComplaintStatus.Reopened, ComplaintStatus.InProgress) => true,
                (ComplaintStatus.InProgress, ComplaintStatus.ResolutionProposed) => true,
                (ComplaintStatus.Reopened, ComplaintStatus.ResolutionProposed) => true,
                _ => false
            }
            : isTenant && (current, next) switch
            {
                (ComplaintStatus.ResolutionProposed, ComplaintStatus.Resolved) => true,
                (ComplaintStatus.ResolutionProposed, ComplaintStatus.Reopened) => true,
                (ComplaintStatus.Resolved, ComplaintStatus.Reopened) => true,
                (ComplaintStatus.Resolved, ComplaintStatus.Closed) => true,
                _ => false
            };

        if (!allowed)
            throw new BadRequestException($"The transition from {current} to {next} is not allowed for your role.");
        if (next is ComplaintStatus.ResolutionProposed or ComplaintStatus.Reopened &&
            string.IsNullOrWhiteSpace(remarks))
            throw new BadRequestException("A resolution summary or reopen reason is required.");
    }

    private static ComplaintActivity CreateActivity(
        Guid complaintId,
        Guid actorId,
        string actorName,
        string actorRole,
        string type,
        ComplaintStatus? previous,
        ComplaintStatus? next,
        string? summary) =>
        new()
        {
            ComplaintActivityId = Guid.NewGuid(),
            ComplaintId = complaintId,
            ActorId = actorId,
            ActorName = actorName,
            ActorRole = actorRole,
            ActivityType = type,
            PreviousStatus = previous,
            NewStatus = next,
            Summary = summary,
            CreatedAt = DateTime.UtcNow
        };

    private Task SendStatusNotificationAsync(
        Guid recipientId,
        Complaint complaint,
        ComplaintStatus status,
        CancellationToken ct)
    {
        var (title, message, type) = status switch
        {
            ComplaintStatus.InProgress =>
                ("Complaint In Progress", $"Work has started on '{complaint.Title}'.", NotificationType.ComplaintUpdated),
            ComplaintStatus.ResolutionProposed =>
                ("Please Review the Fix", $"A resolution was proposed for '{complaint.Title}'.", NotificationType.ComplaintUpdated),
            ComplaintStatus.Reopened =>
                ("Complaint Reopened", $"'{complaint.Title}' was reopened and needs attention.", NotificationType.ComplaintUpdated),
            ComplaintStatus.Resolved =>
                ("Resolution Confirmed", $"'{complaint.Title}' was confirmed as resolved.", NotificationType.ComplaintResolved),
            _ =>
                ("Complaint Updated", $"'{complaint.Title}' was updated to {status}.", NotificationType.ComplaintUpdated)
        };
        return _notificationSender.SendAsync(new Notification
        {
            NotificationId = Guid.NewGuid(),
            UserId = recipientId,
            Type = type,
            Title = title,
            Message = message,
            EntityId = complaint.ComplaintId.ToString(),
            EntityType = "Complaint",
            IsRead = false,
            CreatedAt = DateTime.UtcNow
        }, ct);
    }

    public async Task<ComplaintResponse> GetByIdAsync(Guid complaintId, Guid userId, CancellationToken ct = default)
    {
        var complaint = await _complaintRepo.GetWithDetailsByIdAsync(complaintId);
        if (complaint == null)
        {
            throw new NotFoundException("Complaint", complaintId);
        }

        var user = await _userRepo.GetByIdWithRolesAsync(userId);
        if (user == null)
        {
            throw new NotFoundException("User", userId);
        }

        bool isAdmin = user.UserRoles.Any(ur => ur.Role != null && ur.Role.Name == Roles.Admin);
        bool isTenant = complaint.Lease?.TenantId == userId;
        bool isOwner = complaint.Lease?.Property != null && complaint.Lease.Property.OwnerId == userId;

        if (!isAdmin && !isTenant && !isOwner)
        {
            throw new ForbiddenException("You do not have permission to view this complaint.");
        }

        return await _urlResolver.ResolveAsync(_mapper.MapToResponse(complaint), ct);
    }

    public async Task<IEnumerable<ComplaintResponse>> GetTenantComplaintsAsync(Guid tenantId, CancellationToken ct = default)
    {
        var complaints = await _complaintRepo.GetByTenantIdAsync(tenantId);
        var result = new List<ComplaintResponse>();
        foreach (var c in complaints)
            result.Add(await _urlResolver.ResolveAsync(_mapper.MapToResponse(c), ct));
        return result;
    }

    public async Task<IEnumerable<ComplaintResponse>> GetOwnerComplaintsAsync(Guid ownerId, CancellationToken ct = default)
    {
        var complaints = await _complaintRepo.GetByOwnerIdAsync(ownerId);
        var result = new List<ComplaintResponse>();
        foreach (var c in complaints)
            result.Add(await _urlResolver.ResolveAsync(_mapper.MapToResponse(c), ct));
        return result;
    }

    public async Task<ComplaintResponse> UploadComplaintImageAsync(Guid tenantId, Guid complaintId, IFormFile file, CancellationToken ct = default)
    {
        var complaint = await _complaintRepo.GetWithDetailsByIdAsync(complaintId);
        if (complaint == null)
        {
            throw new NotFoundException("Complaint", complaintId);
        }

        if (complaint.Lease?.TenantId != tenantId)
        {
            throw new ForbiddenException("You are not authorized to upload images for this complaint.");
        }

        if (file == null || file.Length == 0)
        {
            throw new BadRequestException("Invalid or empty file provided.");
        }

        string imagePath = await _storageService.SaveFileAsync(file, "complaints", ct);

        var complaintImage = new ComplaintImage
        {
            ComplaintId = complaintId,
            Image = imagePath,
            CreatedAt = DateTime.UtcNow
        };

        complaint.ComplaintImages.Add(complaintImage);
        // await _complaintRepo.UpdateAsync(complaint);
        await _unitOfWork.SaveChangesAsync(ct);

        return _mapper.MapToResponse(complaint);
    }

    public async Task<PagedResponse<ComplaintResponse>> GetPagedForAdminAsync(
        string? status, string? priority, int page, int limit, CancellationToken ct = default)
    {
        var (items, total) = await _complaintRepo.GetPagedForAdminAsync(status, priority, page, limit, ct);
        return new PagedResponse<ComplaintResponse>
        {
            Data = items.Select(_mapper.MapToResponse).ToList(),
            Total = total, Page = page, Limit = limit
        };
    }

    public async Task MarkOwnerViewedAsync(Guid ownerId, Guid complaintId, CancellationToken ct = default)
    {
        var complaint = await _complaintRepo.GetWithDetailsByIdAsync(complaintId)
            ?? throw new NotFoundException("Complaint", complaintId);
        if (complaint.Lease?.Property?.OwnerId != ownerId)
            throw new ForbiddenException("Only the property owner can acknowledge this complaint.");
        if (complaint.OwnerViewedAt.HasValue) return;

        complaint.OwnerViewedAt = DateTime.UtcNow;
        if (complaint.EscalatedAt.HasValue)
            complaint.EscalationResolvedAt = DateTime.UtcNow;
        complaint.UpdatedAt = DateTime.UtcNow;

        var owner = await _userRepo.GetByIdAsync(ownerId);
        var activity = CreateActivity(
            complaintId, ownerId, owner?.FullName ?? "Owner", Roles.Owner,
            "OwnerViewed", complaint.Status, complaint.Status, "Owner opened the complaint");
        activity.Complaint = complaint;
        await _db.ComplaintActivities.AddAsync(activity, ct);
        await _unitOfWork.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyCollection<ComplaintEscalationResponse>> GetUnattendedForAdminAsync(
        CancellationToken ct = default)
    {
        var cutoff = DateTime.UtcNow.AddHours(-48);
        var now = DateTime.UtcNow;
        return await (
            from complaint in _db.Complaints.AsNoTracking()
            join lease in _db.Leases.AsNoTracking() on complaint.LeaseId equals lease.LeaseId
            join property in _db.Properties.AsNoTracking() on lease.PropertyId equals property.PropertyId
            join owner in _db.Users.AsNoTracking() on property.OwnerId equals owner.UserId
            join tenant in _db.Users.AsNoTracking() on lease.TenantId equals tenant.UserId
            where complaint.Status == ComplaintStatus.Open &&
                  complaint.OwnerViewedAt == null &&
                  complaint.CreatedAt <= cutoff
            orderby complaint.Priority descending, complaint.CreatedAt
            select new ComplaintEscalationResponse
            {
                ComplaintId = complaint.ComplaintId,
                Title = complaint.Title,
                Priority = complaint.Priority.ToString(),
                Status = complaint.Status.ToString(),
                PropertyTitle = property.Title,
                OwnerName = owner.FullName,
                OwnerEmail = owner.Email,
                OwnerPhone = owner.PhoneNumber,
                TenantName = tenant.FullName,
                CreatedAt = complaint.CreatedAt,
                EscalatedAt = complaint.EscalatedAt,
                HoursUnattended = (now - complaint.CreatedAt).TotalHours,
                LastContactedAt = _db.ComplaintEscalationContacts
                    .Where(x => x.ComplaintId == complaint.ComplaintId)
                    .Max(x => (DateTime?)x.ContactedAt),
                ContactCount = _db.ComplaintEscalationContacts
                    .Count(x => x.ComplaintId == complaint.ComplaintId),
                LastContactReason = _db.ComplaintEscalationContacts
                    .Where(x => x.ComplaintId == complaint.ComplaintId)
                    .OrderByDescending(x => x.ContactedAt)
                    .Select(x => x.InternalNote)
                    .FirstOrDefault()
            }).ToArrayAsync(ct);
    }

    public async Task RecordAdminContactAsync(
        Guid adminId,
        Guid complaintId,
        RecordComplaintContactRequest request,
        CancellationToken ct = default)
    {
        var reason = request.Reason.Trim();
        if (string.IsNullOrWhiteSpace(reason))
            throw new BadRequestException("Reason for the owner not opening the complaint is required.");
        if (reason.Length > 1000)
            throw new BadRequestException("Reason must not exceed 1000 characters.");

        var complaintExists = await _db.Complaints.AnyAsync(x => x.ComplaintId == complaintId, ct);
        if (!complaintExists) throw new NotFoundException("Complaint", complaintId);
        var admin = await _userRepo.GetByIdAsync(adminId)
            ?? throw new NotFoundException("User", adminId);

        await _db.ComplaintEscalationContacts.AddAsync(new ComplaintEscalationContact
        {
            ComplaintEscalationContactId = Guid.NewGuid(),
            ComplaintId = complaintId,
            AdminId = adminId,
            AdminName = admin.FullName,
            Method = "Contact",
            InternalNote = reason,
            ContactedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        }, ct);
        await _unitOfWork.SaveChangesAsync(ct);
    }
}
