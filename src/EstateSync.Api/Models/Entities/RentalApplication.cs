using System;
using System.Collections.Generic;
using EstateSync.Api.Common;

namespace EstateSync.Api.Models;

public class RentalApplication : BaseEntity
{
    public Guid ApplicationId { get; set; }
    public Guid PropertyId { get; set; }
    public Property? Property { get; set; }
    public Guid TenantId { get; set; }
    public User? Tenant { get; set; }
    public required string Message { get; set; }
    public int DocumentCount { get; set; }
    public DateOnly ExpectedMoveInDate { get; set; }
    public int DurationMonths { get; set; }
    public decimal CommuteDistance { get; set; }
    public ApplicationStatus Status { get; set; }
    public string? ReviewRemarks { get; set; }
    public DateTime? ReviewedAt { get; set; }

    public ICollection<TenantVerificationRequest> TenantVerificationRequests { get; set; } = new List<TenantVerificationRequest>();
    public ICollection<Lease> Leases { get; set; } = new List<Lease>(); // one or more lease can be exist. for example, tenant can apply for 1 year lease and then extend it for another year, in this case, there will be 2 leases for the same application.
}
