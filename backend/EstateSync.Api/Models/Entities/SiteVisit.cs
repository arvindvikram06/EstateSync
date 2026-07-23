using System;
using EstateSync.Api.Common;

namespace EstateSync.Api.Models;

public class SiteVisit : BaseEntity
{
    public Guid SiteVisitId { get; set; }
    public Guid PropertyId { get; set; }
    public Property? Property { get; set; }
    public Guid TenantId { get; set; }
    public User? Tenant { get; set; }
    public Guid? PropertyVisitSlotId { get; set; }
    public PropertyVisitSlot? PropertyVisitSlot { get; set; }
    public Guid? PreviousPropertyVisitSlotId { get; set; }
    public PropertyVisitSlot? PreviousPropertyVisitSlot { get; set; }
    public DateTime VisitDate { get; set; }
    public TimeOnly VisitTime { get; set; }
    public SiteVisitStatus Status { get; set; }
    public string? OwnerNotes { get; set; }
    public Guid? RescheduledByUserId { get; set; }
    public DateTime? RescheduledAt { get; set; }
    public string? RescheduleReason { get; set; }
}
