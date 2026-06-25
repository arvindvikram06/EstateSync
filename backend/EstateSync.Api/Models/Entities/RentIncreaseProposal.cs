using System;
using EstateSync.Api.Common;

namespace EstateSync.Api.Models;

public class RentIncreaseProposal : BaseEntity
{
    public Guid ProposalId { get; set; }
    public Guid LeaseId { get; set; }
    public Lease? Lease { get; set; }
    public decimal CurrentRent { get; set; }
    public decimal NewRent { get; set; }
    public DateTime EffectiveDate { get; set; }
    public RentIncreaseStatus Status { get; set; }
    public required string Remarks { get; set; }
    public Guid ProposedById { get; set; }
    public User? ProposedBy { get; set; }
}
