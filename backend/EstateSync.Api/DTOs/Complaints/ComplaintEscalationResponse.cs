namespace EstateSync.Api.DTOs.Complaints;

public sealed class ComplaintEscalationResponse
{
    public Guid ComplaintId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Priority { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string PropertyTitle { get; set; } = string.Empty;
    public string OwnerName { get; set; } = string.Empty;
    public string OwnerEmail { get; set; } = string.Empty;
    public string OwnerPhone { get; set; } = string.Empty;
    public string TenantName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? EscalatedAt { get; set; }
    public double HoursUnattended { get; set; }
    public DateTime? LastContactedAt { get; set; }
    public int ContactCount { get; set; }
    public string? LastContactReason { get; set; }
}

public sealed class RecordComplaintContactRequest
{
    public string Reason { get; set; } = string.Empty;
}
