using EstateSync.Api.Common;

namespace EstateSync.Api.Models;

public class PropertyVisitSlot : BaseEntity
{
    public Guid PropertyVisitSlotId { get; set; }
    public Guid PropertyId { get; set; }
    public Property? Property { get; set; }
    public DateTime SlotDate { get; set; }
    public TimeOnly StartTime { get; set; }
    public int DurationMinutes { get; set; } = 60;
    public VisitSlotStatus Status { get; set; }
}
