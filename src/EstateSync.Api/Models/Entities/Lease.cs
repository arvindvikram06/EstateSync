using System;
using System.Collections.Generic;
using EstateSync.Api.Common;

namespace EstateSync.Api.Models;

public class Lease : BaseEntity
{
    public Guid LeaseId { get; set; }

    public Guid PropertyId { get; set; }
    public Property? Property { get; set; }

    public Guid TenantId { get; set; }
    public User? Tenant { get; set; }

    public Guid ApplicationId { get; set; }
    public RentalApplication? RentalApplication { get; set; }

    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }

    public decimal MonthlyRent { get; set; }
    public decimal SecurityDeposit { get; set; }

    public PaymentFrequency PaymentFrequency { get; set; }

    public int? NoticePeriodDays { get; set; }
    public decimal? PenaltyPercentage { get; set; }

    public string? AgreementText { get; set; }
    public string? UnsignedAgreementUrl { get; set; }
    public string? SignedAgreementUrl { get; set; }

    public DateTime? TenantSignedAt { get; set; }
    public string? TenantSignature { get; set; }
    public DateTime? OwnerSignedAt { get; set; }
    public string? OwnerSignature { get; set; }

    public LeaseStatus Status { get; set; }

    public DateTime? ActivatedAt { get; set; }

    public Guid? CancelledBy { get; set; }
    public User? CancelledByUser { get; set; }
    public string? CancellationReason { get; set; }
    public DateTime? CancelledAt { get; set; }

    public bool IsRenewalRequested { get; set; }

    public ICollection<LeaseDocument> LeaseDocuments { get; set; } = new List<LeaseDocument>();
    public ICollection<LeaseBill> LeaseBills { get; set; } = new List<LeaseBill>();
    public ICollection<RentIncreaseProposal> RentIncreaseProposals { get; set; } = new List<RentIncreaseProposal>();
    public ICollection<Complaint> Complaints { get; set; } = new List<Complaint>();
}
