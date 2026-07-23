using EstateSync.Api.Data;
using EstateSync.Api.Models;
using EstateSync.Api.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EstateSync.Api.Repositories;

public sealed class PropertyVisitSlotRepository : BaseRepository<PropertyVisitSlot>, IPropertyVisitSlotRepository
{
    public PropertyVisitSlotRepository(AppDbContext context) : base(context) { }

    public Task<PropertyVisitSlot?> GetWithPropertyAsync(Guid slotId, CancellationToken ct = default) =>
        _dbSet.Include(x => x.Property)
            .FirstOrDefaultAsync(x => x.PropertyVisitSlotId == slotId, ct);

    public async Task<IReadOnlyList<PropertyVisitSlot>> GetForManagementAsync(
        Guid propertyId, DateTime from, DateTime to, CancellationToken ct = default) =>
        await _dbSet.AsNoTracking()
            .Where(x => x.PropertyId == propertyId && x.SlotDate >= from.Date && x.SlotDate <= to.Date)
            .OrderBy(x => x.SlotDate).ThenBy(x => x.StartTime)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<PropertyVisitSlot>> GetAvailableAsync(
        Guid propertyId, DateTime from, DateTime to, CancellationToken ct = default) =>
        await _dbSet.AsNoTracking()
            .Where(x => x.PropertyId == propertyId &&
                        x.SlotDate >= from.Date && x.SlotDate <= to.Date &&
                        x.Status == VisitSlotStatus.Available)
            .OrderBy(x => x.SlotDate).ThenBy(x => x.StartTime)
            .ToListAsync(ct);

    public Task<bool> ExistsAsync(Guid propertyId, DateTime date, TimeOnly time, CancellationToken ct = default) =>
        _dbSet.AnyAsync(x => x.PropertyId == propertyId &&
                             x.SlotDate.Date == date.Date &&
                             x.StartTime == time, ct);

    public async Task<bool> TryReserveAsync(Guid slotId, CancellationToken ct = default) =>
        await _dbSet
            .Where(x => x.PropertyVisitSlotId == slotId && x.Status == VisitSlotStatus.Available)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(x => x.Status, VisitSlotStatus.Reserved)
                .SetProperty(x => x.UpdatedAt, DateTime.UtcNow), ct) == 1;
}
