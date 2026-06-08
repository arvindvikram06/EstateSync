using System;
using System.Collections.Generic;
using EstateSync.Api.Common;

namespace EstateSync.Api.Models;

public class TenantVerificationRequest : BaseEntity
{
    public Guid RequestId { get; set; }
    public Guid TenantId { get; set; }
    public User? Tenant { get; set; }
    public Guid ApplicationId { get; set; }
    public RentalApplication? RentalApplication { get; set; }
    public ApprovalStatus Status { get; set; }
    public Guid? ReviewedBy { get; set; }
    public User? ReviewedByUser { get; set; }
    public string? ReviewRemarks { get; set; }

    public ICollection<TenantVerificationDocument> TenantVerificationDocuments { get; set; } = new List<TenantVerificationDocument>();
}
