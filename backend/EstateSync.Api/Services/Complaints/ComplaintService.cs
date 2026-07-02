using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using EstateSync.Api.Common;
using EstateSync.Api.DTOs.Complaints;
using EstateSync.Api.Exceptions;
using EstateSync.Api.Interfaces.Services.Complaints;
using EstateSync.Api.Interfaces.Services.Notifications;
using EstateSync.Api.Interfaces.Services.Storage;
using EstateSync.Api.Mappers;
using EstateSync.Api.Models;
using EstateSync.Api.Repositories.Interfaces;

namespace EstateSync.Api.Services.Complaints;

public class ComplaintService : IComplaintService
{
    private readonly IComplaintRepository _complaintRepo;
    private readonly ILeaseRepository _leaseRepo;
    private readonly IUserRepository _userRepo;
    private readonly IFileStorageService _storageService;
    private readonly INotificationSender _notificationSender;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ComplaintMapper _mapper;

    public ComplaintService(
        IComplaintRepository complaintRepo,
        ILeaseRepository leaseRepo,
        IUserRepository userRepo,
        IFileStorageService storageService,
        INotificationSender notificationSender,
        IUnitOfWork unitOfWork,
        ComplaintMapper mapper)
    {
        _complaintRepo = complaintRepo ?? throw new ArgumentNullException(nameof(complaintRepo));
        _leaseRepo = leaseRepo ?? throw new ArgumentNullException(nameof(leaseRepo));
        _userRepo = userRepo ?? throw new ArgumentNullException(nameof(userRepo));
        _storageService = storageService ?? throw new ArgumentNullException(nameof(storageService));
        _notificationSender = notificationSender ?? throw new ArgumentNullException(nameof(notificationSender));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
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

        bool isAdmin = user.UserRoles.Any(ur => ur.Role != null && ur.Role.Name == "Admin");
        bool isTenant = complaint.Lease?.TenantId == userId;
        bool isOwner = complaint.Lease?.Property != null && complaint.Lease.Property.OwnerId == userId;

        if (!isAdmin && !isTenant && !isOwner)
        {
            throw new ForbiddenException("You are not authorized to update this complaint.");
        }

        // Validate state transitions
        if (isTenant && !isAdmin)
        {
            // Tenants can only Close their complaints or mark Resolved complaints as Closed
            if (request.Status != ComplaintStatus.Closed)
            {
                throw new BadRequestException("Tenants can only transition complaints to Closed status.");
            }
        }

        complaint.Status = request.Status;
        if (!string.IsNullOrWhiteSpace(request.ReviewRemarks))
        {
            complaint.ReviewRemarks = request.ReviewRemarks;
        }
        complaint.UpdatedAt = DateTime.UtcNow;

        var tenantId = complaint.Lease?.TenantId;
        if (tenantId.HasValue)
        {
            var notificationType = request.Status == ComplaintStatus.Resolved
                ? NotificationType.ComplaintResolved
                : NotificationType.ComplaintUpdated;

            var title = request.Status == ComplaintStatus.Resolved
                ? "Complaint Resolved"
                : "Complaint Status Updated";

            await _notificationSender.SendAsync(new Notification
            {
                NotificationId = Guid.NewGuid(),
                UserId = tenantId.Value,
                Type = notificationType,
                Title = title,
                Message = $"Your complaint '{complaint.Title}' status has been updated to {request.Status}." +
                          (string.IsNullOrWhiteSpace(request.ReviewRemarks) ? "" : $" Remarks: {request.ReviewRemarks}"),
                EntityId = complaint.ComplaintId.ToString(),
                EntityType = "Complaint",
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            });
        }

        // await _complaintRepo.UpdateAsync(complaint);
        await _unitOfWork.SaveChangesAsync(ct);

        return _mapper.MapToResponse(complaint);
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

        bool isAdmin = user.UserRoles.Any(ur => ur.Role != null && ur.Role.Name == "Admin");
        bool isTenant = complaint.Lease?.TenantId == userId;
        bool isOwner = complaint.Lease?.Property != null && complaint.Lease.Property.OwnerId == userId;

        if (!isAdmin && !isTenant && !isOwner)
        {
            throw new ForbiddenException("You do not have permission to view this complaint.");
        }

        return _mapper.MapToResponse(complaint);
    }

    public async Task<IEnumerable<ComplaintResponse>> GetMyComplaintsAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await _userRepo.GetByIdWithRolesAsync(userId);
        if (user == null)
        {
            throw new NotFoundException("User", userId);
        }

        bool isAdmin = user.UserRoles.Any(ur => ur.Role != null && ur.Role.Name == "Admin");
        bool isOwner = user.UserRoles.Any(ur => ur.Role != null && ur.Role.Name == "Owner");

        IEnumerable<Complaint> complaints;

        if (isAdmin)
        {
            complaints = await _complaintRepo.GetAllAsync();
        }
        else if (isOwner)
        {
            complaints = await _complaintRepo.GetByOwnerIdAsync(userId);
        }
        else
        {
            complaints = await _complaintRepo.GetByTenantIdAsync(userId);
        }

        return complaints.Select(_mapper.MapToResponse);
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
}
