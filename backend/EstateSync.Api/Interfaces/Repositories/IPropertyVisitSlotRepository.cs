using EstateSync.Api.Models;

namespace EstateSync.Api.Repositories.Interfaces;

public interface IPropertyVisitSlotRepository : IRepository<PropertyVisitSlot>
{
    Task<PropertyVisitSlot?> GetWithPropertyAsync(Guid slotId, CancellationToken ct = default);
    Task<IReadOnlyList<PropertyVisitSlot>> GetForManagementAsync(Guid propertyId, DateTime from, DateTime to, CancellationToken ct = default);
    Task<IReadOnlyList<PropertyVisitSlot>> GetAvailableAsync(Guid propertyId, DateTime from, DateTime to, CancellationToken ct = default);
    Task<bool> ExistsAsync(Guid propertyId, DateTime date, TimeOnly time, CancellationToken ct = default);
    Task<bool> TryReserveAsync(Guid slotId, CancellationToken ct = default);
}
