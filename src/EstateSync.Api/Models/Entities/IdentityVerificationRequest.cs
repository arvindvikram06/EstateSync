using System;
using System.Collections.Generic;
using EstateSync.Api.Common;

namespace EstateSync.Api.Models;

public class IdentityVerificationRequest : BaseEntity
{
    public Guid RequestId { get; set; }
    public Guid UserId { get; set; }
    public User? User { get; set; }
    
    public string? AadhaarNumber { get; set; }
    public string? PanNumber { get; set; }
    
    public ApprovalStatus Status { get; set; }
    public Guid? ReviewedById { get; set; }
    public User? ReviewedBy { get; set; }
    public string? ReviewRemarks { get; set; }

    public ICollection<IdentityVerificationDocument> IdentityVerificationDocuments { get; set; } = new List<IdentityVerificationDocument>();
}
