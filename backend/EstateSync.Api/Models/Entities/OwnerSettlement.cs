using System;
using EstateSync.Api.Common;

namespace EstateSync.Api.Models;

public class OwnerSettlement : BaseEntity
{
    public Guid OwnerSettlementId { get; set; }
    public Guid OwnerId { get; set; }
    public User? Owner { get; set; }

    public required string StripePayoutId { get; set; }
    public decimal Amount { get; set; }
    public required string Currency { get; set; }
    public required string DestinationId { get; set; }
    
    public DateTime ArrivalDate { get; set; }
    public required string Status { get; set; }
}
