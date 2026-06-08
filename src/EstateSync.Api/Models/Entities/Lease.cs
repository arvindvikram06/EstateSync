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

    public Guid AgreementTypeId { get; set; }
    public AgreementType? AgreementType { get; set; }

    public Guid TemplateId { get; set; }
    public AgreementTemplate? Template { get; set; }

    public Guid ApplicationId { get; set; }
    public RentalApplication? RentalApplication { get; set; }

    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }

    public decimal MonthlyRent { get; set; }
    public decimal SecurityDeposit { get; set; }

    public PaymentFrequency PaymentFrequency { get; set; }

    public int? NoticePeriodDays { get; set; }
    public decimal? PenaltyPercentage { get; set; }

    public string? AgreementData { get; set; }
    public string? FinalAgreementHtml { get; set; }
    public string? AgreementDocumentUrl { get; set; }

    public LeaseStatus Status { get; set; }

    public DateTime? ActivatedAt { get; set; }

    public Guid? CancelledBy { get; set; }
    public User? CancelledByUser { get; set; }
    public string? CancellationReason { get; set; }
    public DateTime? CancelledAt { get; set; }

    public ICollection<LeaseDocument> LeaseDocuments { get; set; } = new List<LeaseDocument>();
    public ICollection<AgreementAcknowledgement> AgreementAcknowledgements { get; set; } = new List<AgreementAcknowledgement>();
    public ICollection<Charge> Charges { get; set; } = new List<Charge>();
    public ICollection<RentIncreaseProposal> RentIncreaseProposals { get; set; } = new List<RentIncreaseProposal>();
    public ICollection<Complaint> Complaints { get; set; } = new List<Complaint>();
}
