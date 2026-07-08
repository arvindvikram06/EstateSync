using System;

namespace EstateSync.Api.DTOs.RentalApplications;

public record RentalApplicationResponse
{
    public Guid ApplicationId { get; init; }
    public Guid PropertyId { get; init; }
    public string? PropertyTitle { get; init; }
    public Guid TenantId { get; init; }
    public string? TenantName { get; init; }
    public string Message { get; init; } = string.Empty;
    public DateOnly ExpectedMoveInDate { get; init; }
    public int DurationMonths { get; init; }
    public decimal CommuteDistance { get; init; }
    public string Status { get; init; } = string.Empty;
    public Guid? LeaseId { get; init; }
    public string? LeaseStatus { get; init; }
    public string? ReviewRemarks { get; init; }
    public DateTime? ReviewedAt { get; init; }
    public DateTime CreatedAt { get; init; }
}
