using System;
using System.Collections.Generic;
using EstateSync.Api.Common;

namespace EstateSync.Api.Models;

public class Complaint : BaseEntity
{
    public Guid ComplaintId { get; set; }
    public Guid LeaseId { get; set; }
    public Lease? Lease { get; set; }
    public Guid? ChargeId { get; set; }
    public Charge? Charge { get; set; }
    public required string Title { get; set; }
    public required string Description { get; set; }
    public ComplaintType ComplaintType { get; set; }
    public ComplaintPriority Priority { get; set; }
    public ComplaintStatus Status { get; set; }
    public string? ReviewRemarks { get; set; }

    public ICollection<ComplaintImage> ComplaintImages { get; set; } = new List<ComplaintImage>();
}
