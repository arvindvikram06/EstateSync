using System;

namespace EstateSync.Api.DTOs.Leases;

public record LeaseVacateNoticeResponse
{
    public Guid VacateNoticeId { get; init; }
    public Guid LeaseId { get; init; }
    public DateTime RequestedExitDate { get; init; }
    public string Status { get; init; } = string.Empty;
    public bool PenaltyApplied { get; init; }
    public decimal PenaltyAmount { get; init; }
    public string? RejectionReason { get; init; }
    public DateTime CreatedAt { get; init; }
}
