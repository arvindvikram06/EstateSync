using System;
using System.Collections.Generic;
using EstateSync.Api.Common;

namespace EstateSync.Api.Models;

public class KycVerification : BaseEntity
{
    public Guid KycVerificationId { get; set; }
    public Guid UserId { get; set; }
    public User? User { get; set; }

    public string? AadhaarNumber { get; set; }
    public string? PanNumber { get; set; }

    public ApprovalStatus Status { get; set; }
    public Guid? ReviewedById { get; set; }
    public User? ReviewedBy { get; set; }
    public string? ReviewRemarks { get; set; }
    public DateTime? ReviewedAt { get; set; }

    public ICollection<KycDocument> KycDocuments { get; set; } = new List<KycDocument>();
}
