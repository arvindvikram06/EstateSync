using EstateSync.Api.Common;

namespace EstateSync.Api.Models;

public sealed class ComplaintEscalationContact : BaseEntity
{
    public Guid ComplaintEscalationContactId { get; set; }
    public Guid ComplaintId { get; set; }
    public Complaint? Complaint { get; set; }
    public Guid AdminId { get; set; }
    public string AdminName { get; set; } = string.Empty;
    public string Method { get; set; } = string.Empty;
    public string? InternalNote { get; set; }
    public DateTime ContactedAt { get; set; }
}
