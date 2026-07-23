using EstateSync.Api.DTOs.SiteVisits;
using EstateSync.Api.Data;
using EstateSync.Api.Exceptions;
using EstateSync.Api.Interfaces.Services.Notifications;
using EstateSync.Api.Interfaces.Services.SiteVisits;
using EstateSync.Api.Mappers;
using EstateSync.Api.Models;
using EstateSync.Api.Repositories.Interfaces;

namespace EstateSync.Api.Services.SiteVisits;

public class SiteVisitService : ISiteVisitService
{
    private readonly ISiteVisitRepository _visitRepo;
    private readonly AppDbContext _db;
    private readonly IPropertyVisitSlotRepository _slotRepo;
    private readonly IPropertyRepository _propertyRepo;
    private readonly INotificationSender _notifications;
    private readonly IUnitOfWork _unitOfWork;
    private readonly SiteVisitMapper _mapper;

    public SiteVisitService(
        ISiteVisitRepository visitRepo,
        AppDbContext db,
        IPropertyVisitSlotRepository slotRepo,
        IPropertyRepository propertyRepo,
        INotificationSender notifications,
        IUnitOfWork unitOfWork,
        SiteVisitMapper mapper)
    {
        _visitRepo = visitRepo;
        _db = db;
        _slotRepo = slotRepo;
        _propertyRepo = propertyRepo;
        _notifications = notifications;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<SiteVisitResponse> RequestVisitAsync(
        Guid tenantId, CreateSiteVisitRequest request, CancellationToken ct = default)
    {
        await using var transaction = await _db.Database.BeginTransactionAsync(ct);
        var property = await _propertyRepo.GetByIdAsync(request.PropertyId)
            ?? throw new NotFoundException("Property", request.PropertyId);
        if (property.OwnerId == tenantId)
            throw new ForbiddenException("Owners cannot book visits on their own properties.");
        if (await _visitRepo.HasActiveRequestAsync(tenantId, request.PropertyId))
            throw new ConflictException("You already have an active visit request for this property.");

        var slot = await RequireAvailableSlotAsync(request.PropertyId, request.SlotId, ct);
        slot.Status = VisitSlotStatus.Reserved;
        slot.UpdatedAt = DateTime.UtcNow;

        var visit = new SiteVisit
        {
            SiteVisitId = Guid.NewGuid(),
            PropertyId = request.PropertyId,
            Property = property,
            TenantId = tenantId,
            PropertyVisitSlotId = slot.PropertyVisitSlotId,
            PropertyVisitSlot = slot,
            VisitDate = slot.SlotDate,
            VisitTime = slot.StartTime,
            Status = SiteVisitStatus.PendingOwnerApproval,
            CreatedAt = DateTime.UtcNow
        };
        await _visitRepo.AddAsync(visit);
        await NotifyAsync(property.OwnerId, NotificationType.SiteVisitRequested,
            "New Site Visit Requested",
            $"A visit was requested for '{property.Title}' on {slot.SlotDate:yyyy-MM-dd} at {slot.StartTime:HH:mm}.",
            visit.SiteVisitId, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return _mapper.MapToResponse(visit);
    }

    public async Task<SiteVisitResponse> UpdateStatusAsync(
        Guid userId, Guid siteVisitId, UpdateSiteVisitStatusRequest request, CancellationToken ct = default)
    {
        await using var transaction = await _db.Database.BeginTransactionAsync(ct);
        var visit = await RequireVisitAsync(siteVisitId);
        var isOwner = visit.Property?.OwnerId == userId;
        var isTenant = visit.TenantId == userId;

        if (request.Status == SiteVisitStatus.Approved)
        {
            if (!isOwner || visit.Status != SiteVisitStatus.PendingOwnerApproval || visit.PreviousPropertyVisitSlotId != null)
                throw new ForbiddenException("Only the owner can approve a new tenant visit request.");
            visit.Status = SiteVisitStatus.Approved;
            visit.PropertyVisitSlot!.Status = VisitSlotStatus.Booked;
            visit.OwnerNotes = request.OwnerNotes;
            await NotifyAsync(visit.TenantId, NotificationType.SiteVisitApproved, "Site Visit Confirmed",
                $"Your visit for '{visit.Property?.Title}' is confirmed for {visit.VisitDate:yyyy-MM-dd} at {visit.VisitTime:HH:mm}.",
                visit.SiteVisitId, ct);
        }
        else if (request.Status == SiteVisitStatus.Declined)
        {
            if (!isOwner || visit.Status != SiteVisitStatus.PendingOwnerApproval || visit.PreviousPropertyVisitSlotId != null)
                throw new ForbiddenException("Only the owner can decline a new tenant visit request.");
            visit.Status = SiteVisitStatus.Declined;
            visit.PropertyVisitSlot!.Status = VisitSlotStatus.Available;
            visit.OwnerNotes = request.OwnerNotes;
            await NotifyAsync(visit.TenantId, NotificationType.SiteVisitDeclined, "Site Visit Declined",
                $"Your visit request for '{visit.Property?.Title}' was declined.", visit.SiteVisitId, ct);
        }
        else if (request.Status == SiteVisitStatus.Cancelled)
        {
            if (!isTenant) throw new ForbiddenException("Only the requesting tenant can cancel this visit.");
            if (visit.Status is SiteVisitStatus.Completed or SiteVisitStatus.Cancelled or SiteVisitStatus.Declined)
                throw new ConflictException("This visit can no longer be cancelled.");
            if (visit.PropertyVisitSlot != null) visit.PropertyVisitSlot.Status = VisitSlotStatus.Available;
            if (visit.PreviousPropertyVisitSlot != null) visit.PreviousPropertyVisitSlot.Status = VisitSlotStatus.Available;
            visit.Status = SiteVisitStatus.Cancelled;
            await NotifyAsync(visit.Property!.OwnerId, NotificationType.SiteVisitCancelled, "Site Visit Cancelled",
                $"The visit for '{visit.Property.Title}' was cancelled by the tenant.", visit.SiteVisitId, ct);
        }
        else if (request.Status == SiteVisitStatus.Completed)
        {
            if (!isOwner) throw new ForbiddenException("Only the property owner can complete a visit.");
            if (visit.Status != SiteVisitStatus.Approved) throw new ConflictException("Only approved visits can be completed.");
            visit.Status = SiteVisitStatus.Completed;
        }
        else
        {
            throw new BadRequestException("Invalid visit status transition.");
        }

        visit.UpdatedAt = DateTime.UtcNow;
        await _unitOfWork.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return _mapper.MapToResponse(visit);
    }

    public async Task<SiteVisitResponse> RescheduleAsync(
        Guid userId, Guid siteVisitId, RescheduleSiteVisitRequest request, CancellationToken ct = default)
    {
        await using var transaction = await _db.Database.BeginTransactionAsync(ct);
        var visit = await RequireVisitAsync(siteVisitId);
        var isOwner = visit.Property?.OwnerId == userId;
        var isTenant = visit.TenantId == userId;
        if (!isOwner && !isTenant) throw new ForbiddenException("Only visit participants can reschedule.");
        if (visit.Status != SiteVisitStatus.Approved)
            throw new ConflictException("Only an approved visit can be rescheduled.");

        var newSlot = await RequireAvailableSlotAsync(visit.PropertyId, request.NewSlotId, ct);
        visit.PreviousPropertyVisitSlotId = visit.PropertyVisitSlotId;
        visit.PreviousPropertyVisitSlot = visit.PropertyVisitSlot;
        visit.PropertyVisitSlotId = newSlot.PropertyVisitSlotId;
        visit.PropertyVisitSlot = newSlot;
        visit.VisitDate = newSlot.SlotDate;
        visit.VisitTime = newSlot.StartTime;
        visit.RescheduledByUserId = userId;
        visit.RescheduledAt = DateTime.UtcNow;
        visit.RescheduleReason = request.Reason.Trim();
        visit.Status = isOwner ? SiteVisitStatus.PendingTenantApproval : SiteVisitStatus.PendingOwnerApproval;
        newSlot.Status = VisitSlotStatus.Reserved;

        var recipient = isOwner ? visit.TenantId : visit.Property!.OwnerId;
        await NotifyAsync(recipient, NotificationType.SiteVisitRescheduled, "New Visit Time Proposed",
            $"A new time was proposed for '{visit.Property?.Title}': {visit.VisitDate:yyyy-MM-dd} at {visit.VisitTime:HH:mm}.",
            visit.SiteVisitId, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return _mapper.MapToResponse(visit);
    }

    public async Task<SiteVisitResponse> ConfirmRescheduleAsync(
        Guid userId, Guid siteVisitId, CancellationToken ct = default)
    {
        var visit = await RequireVisitAsync(siteVisitId);
        var canConfirm = (visit.Status == SiteVisitStatus.PendingOwnerApproval && visit.Property?.OwnerId == userId) ||
                         (visit.Status == SiteVisitStatus.PendingTenantApproval && visit.TenantId == userId);
        if (!canConfirm || visit.PreviousPropertyVisitSlot == null)
            throw new ForbiddenException("You cannot confirm this reschedule proposal.");

        visit.PreviousPropertyVisitSlot.Status = VisitSlotStatus.Available;
        visit.PropertyVisitSlot!.Status = VisitSlotStatus.Booked;
        visit.PreviousPropertyVisitSlotId = null;
        visit.PreviousPropertyVisitSlot = null;
        visit.Status = SiteVisitStatus.Approved;
        visit.UpdatedAt = DateTime.UtcNow;
        await _unitOfWork.SaveChangesAsync(ct);
        return _mapper.MapToResponse(visit);
    }

    public async Task<SiteVisitResponse> RejectRescheduleAsync(
        Guid userId, Guid siteVisitId, CancellationToken ct = default)
    {
        var visit = await RequireVisitAsync(siteVisitId);
        var canReject = (visit.Status == SiteVisitStatus.PendingOwnerApproval && visit.Property?.OwnerId == userId) ||
                        (visit.Status == SiteVisitStatus.PendingTenantApproval && visit.TenantId == userId);
        if (!canReject || visit.PreviousPropertyVisitSlot == null)
            throw new ForbiddenException("You cannot reject this reschedule proposal.");

        visit.PropertyVisitSlot!.Status = VisitSlotStatus.Available;
        var restored = visit.PreviousPropertyVisitSlot;
        visit.PropertyVisitSlotId = restored.PropertyVisitSlotId;
        visit.PropertyVisitSlot = restored;
        visit.VisitDate = restored.SlotDate;
        visit.VisitTime = restored.StartTime;
        visit.PreviousPropertyVisitSlotId = null;
        visit.PreviousPropertyVisitSlot = null;
        visit.Status = SiteVisitStatus.Approved;
        visit.UpdatedAt = DateTime.UtcNow;
        await _unitOfWork.SaveChangesAsync(ct);
        return _mapper.MapToResponse(visit);
    }

    public async Task<IEnumerable<SiteVisitResponse>> GetTenantVisitsAsync(Guid tenantId, CancellationToken ct = default) =>
        (await _visitRepo.GetByTenantIdAsync(tenantId)).Select(_mapper.MapToResponse);

    public async Task<IEnumerable<SiteVisitResponse>> GetOwnerVisitsAsync(Guid ownerId, CancellationToken ct = default) =>
        (await _visitRepo.GetByOwnerIdAsync(ownerId)).Select(_mapper.MapToResponse);

    public async Task<SiteVisitResponse> GetByIdAsync(Guid id, Guid userId, CancellationToken ct = default)
    {
        var visit = await RequireVisitAsync(id);
        if (visit.TenantId != userId && visit.Property?.OwnerId != userId)
            throw new ForbiddenException("You are not authorized to view this visit.");
        return _mapper.MapToResponse(visit);
    }

    private async Task<SiteVisit> RequireVisitAsync(Guid id) =>
        await _visitRepo.GetWithDetailsByIdAsync(id) ?? throw new NotFoundException("SiteVisit", id);

    private async Task<PropertyVisitSlot> RequireAvailableSlotAsync(Guid propertyId, Guid slotId, CancellationToken ct)
    {
        var slot = await _slotRepo.GetWithPropertyAsync(slotId, ct);
        if (slot == null || slot.PropertyId != propertyId) throw new NotFoundException("PropertyVisitSlot", slotId);
        if (slot.Status != VisitSlotStatus.Available || !SiteVisitSlotRules.IsFuture(slot.SlotDate, slot.StartTime))
            throw new ConflictException("This visit slot is no longer available.");
        if (!await _slotRepo.TryReserveAsync(slotId, ct))
            throw new ConflictException("This visit slot was just reserved by another tenant.");
        return slot;
    }

    private Task NotifyAsync(Guid userId, NotificationType type, string title, string message, Guid visitId, CancellationToken ct) =>
        _notifications.SendAsync(new Notification
        {
            NotificationId = Guid.NewGuid(), UserId = userId, Type = type, Title = title, Message = message,
            EntityId = visitId.ToString(), EntityType = "SiteVisit", IsRead = false, CreatedAt = DateTime.UtcNow
        }, ct);
}
