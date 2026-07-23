using EstateSync.Api.DTOs.SiteVisits;
using EstateSync.Api.Exceptions;
using EstateSync.Api.Interfaces.Services.SiteVisits;
using EstateSync.Api.Models;
using EstateSync.Api.Repositories.Interfaces;

namespace EstateSync.Api.Services.SiteVisits;

public sealed class PropertyVisitSlotService : IPropertyVisitSlotService
{
    private readonly IPropertyVisitSlotRepository _slotRepo;
    private readonly IPropertyRepository _propertyRepo;
    private readonly IUnitOfWork _unitOfWork;

    public PropertyVisitSlotService(
        IPropertyVisitSlotRepository slotRepo,
        IPropertyRepository propertyRepo,
        IUnitOfWork unitOfWork)
    {
        _slotRepo = slotRepo;
        _propertyRepo = propertyRepo;
        _unitOfWork = unitOfWork;
    }

    public async Task<IReadOnlyCollection<PropertyVisitSlotResponse>> CreateAsync(
        Guid ownerId, Guid propertyId, CreateVisitSlotsRequest request, CancellationToken ct = default)
    {
        await RequireOwnerAsync(ownerId, propertyId);
        if (request.Date.Date < SiteVisitSlotRules.CurrentLocalDateTime().Date)
            throw new BadRequestException("Slot date cannot be in the past.");

        var created = new List<PropertyVisitSlot>();
        foreach (var time in request.StartTimes.Distinct())
        {
            if (!SiteVisitSlotRules.IsSupported(time) || !SiteVisitSlotRules.IsFuture(request.Date, time))
                throw new BadRequestException("Every slot must be a future full-hour slot between 09:00 and 18:00.");
            if (await _slotRepo.ExistsAsync(propertyId, request.Date, time, ct))
                continue;

            var slot = new PropertyVisitSlot
            {
                PropertyVisitSlotId = Guid.NewGuid(),
                PropertyId = propertyId,
                SlotDate = request.Date.Date,
                StartTime = time,
                DurationMinutes = SiteVisitSlotRules.DurationMinutes,
                Status = VisitSlotStatus.Available,
                CreatedAt = DateTime.UtcNow
            };
            await _slotRepo.AddAsync(slot);
            created.Add(slot);
        }

        await _unitOfWork.SaveChangesAsync(ct);
        return created.Select(Map).ToArray();
    }

    public async Task<IReadOnlyCollection<PropertyVisitSlotResponse>> GetManageAsync(
        Guid ownerId, Guid propertyId, DateTime from, DateTime to, CancellationToken ct = default)
    {
        await RequireOwnerAsync(ownerId, propertyId);
        ValidateRange(from, to);
        return (await _slotRepo.GetForManagementAsync(propertyId, from, to, ct)).Select(Map).ToArray();
    }

    public async Task<IReadOnlyCollection<PropertyVisitSlotResponse>> GetAvailableAsync(
        Guid propertyId, DateTime from, DateTime to, CancellationToken ct = default)
    {
        if (await _propertyRepo.GetByIdAsync(propertyId) == null)
            throw new NotFoundException("Property", propertyId);
        ValidateRange(from, to);
        var slots = await _slotRepo.GetAvailableAsync(propertyId, from, to, ct);
        return slots.Where(x => SiteVisitSlotRules.IsFuture(x.SlotDate, x.StartTime)).Select(Map).ToArray();
    }

    public async Task<PropertyVisitSlotResponse> SetBlockedAsync(
        Guid ownerId, Guid propertyId, Guid slotId, bool blocked, CancellationToken ct = default)
    {
        await RequireOwnerAsync(ownerId, propertyId);
        var slot = await RequireSlotAsync(propertyId, slotId, ct);
        var desired = blocked ? VisitSlotStatus.Blocked : VisitSlotStatus.Available;
        if (slot.Status is VisitSlotStatus.Reserved or VisitSlotStatus.Booked)
            throw new ConflictException("Reserved or booked slots cannot be changed.");
        slot.Status = desired;
        slot.UpdatedAt = DateTime.UtcNow;
        await _unitOfWork.SaveChangesAsync(ct);
        return Map(slot);
    }

    public async Task DeleteAsync(Guid ownerId, Guid propertyId, Guid slotId, CancellationToken ct = default)
    {
        await RequireOwnerAsync(ownerId, propertyId);
        var slot = await RequireSlotAsync(propertyId, slotId, ct);
        if (slot.Status is VisitSlotStatus.Reserved or VisitSlotStatus.Booked)
            throw new ConflictException("Reserved or booked slots cannot be deleted.");
        slot.DeletedAt = DateTime.UtcNow;
        await _unitOfWork.SaveChangesAsync(ct);
    }

    private async Task RequireOwnerAsync(Guid ownerId, Guid propertyId)
    {
        var property = await _propertyRepo.GetByIdAsync(propertyId);
        if (property == null) throw new NotFoundException("Property", propertyId);
        if (property.OwnerId != ownerId) throw new ForbiddenException("Only the property owner can manage visit slots.");
    }

    private async Task<PropertyVisitSlot> RequireSlotAsync(Guid propertyId, Guid slotId, CancellationToken ct)
    {
        var slot = await _slotRepo.GetWithPropertyAsync(slotId, ct);
        if (slot == null || slot.PropertyId != propertyId) throw new NotFoundException("PropertyVisitSlot", slotId);
        return slot;
    }

    private static void ValidateRange(DateTime from, DateTime to)
    {
        if (to.Date < from.Date || (to.Date - from.Date).TotalDays > 60)
            throw new BadRequestException("Date range must be between 0 and 60 days.");
    }

    private static PropertyVisitSlotResponse Map(PropertyVisitSlot slot) =>
        new(slot.PropertyVisitSlotId, slot.PropertyId, slot.SlotDate,
            slot.StartTime.ToString("HH:mm"), slot.StartTime.AddMinutes(60).ToString("HH:mm"),
            slot.DurationMinutes, slot.Status.ToString());
}
