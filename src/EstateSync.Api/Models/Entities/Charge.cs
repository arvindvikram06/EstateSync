using System;
using System.Collections.Generic;

namespace EstateSync.Api.Models;

public class Charge
{
    public Guid ChargeId { get; set; }
    public Guid LeaseId { get; set; }
    public Lease? Lease { get; set; }
    public ChargeType ChargeType { get; set; }
    public DateTime DueDate { get; set; }
    public DateTime EndDate { get; set; }
    public decimal Amount { get; set; }
    public decimal PendingAmount { get; set; }
    public DateTime CreatedAt { get; set; }

    public ICollection<Payment> Payments { get; set; } = new List<Payment>();
    public ICollection<Complaint> Complaints { get; set; } = new List<Complaint>();
}
