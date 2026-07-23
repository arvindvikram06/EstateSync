using EstateSync.Api.Common;

namespace EstateSync.Api.Models;

public sealed class ComplaintActivity : BaseEntity
{
    public Guid ComplaintActivityId { get; set; }
    public Guid ComplaintId { get; set; }
    public Complaint? Complaint { get; set; }
    public Guid ActorId { get; set; }
    public string ActorName { get; set; } = string.Empty;
    public string ActorRole { get; set; } = string.Empty;
    public string ActivityType { get; set; } = string.Empty;
    public ComplaintStatus? PreviousStatus { get; set; }
    public ComplaintStatus? NewStatus { get; set; }
    public string? Summary { get; set; }
}
