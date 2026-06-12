using System;
using System.Collections.Generic;
using EstateSync.Api.Common;

namespace EstateSync.Api.Models;

public class LeaseBill : BaseEntity
{
    public Guid LeaseBillId { get; set; }
    public Guid LeaseId { get; set; }
    public Lease? Lease { get; set; }
    public LeaseBillType BillType { get; set; }
    public DateTime DueDate { get; set; }
    public decimal Amount { get; set; }
    public decimal PendingAmount { get; set; }

    public ICollection<Payment> Payments { get; set; } = new List<Payment>();
    public ICollection<Complaint> Complaints { get; set; } = new List<Complaint>();
}
