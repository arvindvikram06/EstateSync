using System;
using System.Collections.Generic;
using EstateSync.Api.Common;

namespace EstateSync.Api.Models;

public class OwnerVerificationRequest : BaseEntity
{
    public Guid RequestId { get; set; }
    public Guid UserId { get; set; }
    public User? User { get; set; }
    public ApprovalStatus Status { get; set; }
    public Guid? ReviewedById { get; set; }
    public User? ReviewedBy { get; set; }
    public string? ReviewRemarks { get; set; }


    public ICollection<OwnerVerificationDocument> OwnerVerificationDocuments { get; set; } = new List<OwnerVerificationDocument>();
}
