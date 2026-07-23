using System;
using System.Collections.Generic;
using EstateSync.Api.Common;

namespace EstateSync.Api.Models;

public class Complaint : BaseEntity
{
    public Guid ComplaintId { get; set; }
    public Guid LeaseId { get; set; }
    public Lease? Lease { get; set; }
    public Guid? LeaseBillId { get; set; }
    public LeaseBill? LeaseBill { get; set; }
    public required string Title { get; set; }
    public required string Description { get; set; }
    public ComplaintType ComplaintType { get; set; }
    public ComplaintPriority Priority { get; set; }
    public ComplaintStatus Status { get; set; }
    public string? ReviewRemarks { get; set; }
    public string? ResolutionSummary { get; set; }
    public DateTime? ResolutionProposedAt { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public int ReopenCount { get; set; }
    public DateTime? LastReopenedAt { get; set; }
    public string? ReopenReason { get; set; }
    public DateTime? OwnerViewedAt { get; set; }
    public DateTime? EscalatedAt { get; set; }
    public DateTime? EscalationResolvedAt { get; set; }

    public ICollection<ComplaintImage> ComplaintImages { get; set; } = new List<ComplaintImage>();
    public ICollection<ComplaintActivity> Activities { get; set; } = new List<ComplaintActivity>();
    public ICollection<ComplaintEscalationContact> EscalationContacts { get; set; } = new List<ComplaintEscalationContact>();
}
