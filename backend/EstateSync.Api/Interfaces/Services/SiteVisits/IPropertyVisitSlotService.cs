using EstateSync.Api.DTOs.SiteVisits;

namespace EstateSync.Api.Interfaces.Services.SiteVisits;

public interface IPropertyVisitSlotService
{
    Task<IReadOnlyCollection<PropertyVisitSlotResponse>> CreateAsync(Guid ownerId, Guid propertyId, CreateVisitSlotsRequest request, CancellationToken ct = default);
    Task<IReadOnlyCollection<PropertyVisitSlotResponse>> GetManageAsync(Guid ownerId, Guid propertyId, DateTime from, DateTime to, CancellationToken ct = default);
    Task<IReadOnlyCollection<PropertyVisitSlotResponse>> GetAvailableAsync(Guid propertyId, DateTime from, DateTime to, CancellationToken ct = default);
    Task<PropertyVisitSlotResponse> SetBlockedAsync(Guid ownerId, Guid propertyId, Guid slotId, bool blocked, CancellationToken ct = default);
    Task DeleteAsync(Guid ownerId, Guid propertyId, Guid slotId, CancellationToken ct = default);
}
