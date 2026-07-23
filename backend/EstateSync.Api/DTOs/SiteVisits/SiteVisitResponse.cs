using System;

namespace EstateSync.Api.DTOs.SiteVisits;

public class SiteVisitResponse
{
    public Guid SiteVisitId { get; set; }
    public Guid PropertyId { get; set; }
    public string? PropertyTitle { get; set; }
    public Guid TenantId { get; set; }
    public string? TenantName { get; set; }
    public Guid? SlotId { get; set; }
    public Guid? PreviousSlotId { get; set; }
    public DateTime VisitDate { get; set; }
    public string VisitTime { get; set; } = string.Empty; // Formatted time string, e.g., "14:30"
    public string Status { get; set; } = string.Empty; // SiteVisitStatus string value
    public string? OwnerNotes { get; set; }
    public Guid? RescheduledByUserId { get; set; }
    public DateTime? RescheduledAt { get; set; }
    public string? RescheduleReason { get; set; }
    public int DurationMinutes { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
