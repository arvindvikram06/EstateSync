using System;
using EstateSync.Api.Common;

namespace EstateSync.Api.Models;

public class LeaseVacateNotice : BaseEntity
{
    public Guid VacateNoticeId { get; set; }

    public Guid LeaseId { get; set; }
    public Lease? Lease { get; set; }

    public Guid TenantId { get; set; }
    public User? Tenant { get; set; }

    public DateTime RequestedExitDate { get; set; }
    public VacateNoticeStatus Status { get; set; }

    public bool PenaltyApplied { get; set; }
    public decimal PenaltyAmount { get; set; }

    public string? RejectionReason { get; set; }
}
