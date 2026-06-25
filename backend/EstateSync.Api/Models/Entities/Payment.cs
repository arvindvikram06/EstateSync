using System;
using EstateSync.Api.Common;

namespace EstateSync.Api.Models;

public class Payment : BaseEntity
{
    public Guid PaymentId { get; set; }
    public Guid LeaseBillId { get; set; }
    public LeaseBill? LeaseBill { get; set; }
    public Guid PaidByUserId { get; set; }
    public User? PaidByUser { get; set; }
    public decimal Amount { get; set; }
    public decimal PlatformFee { get; set; }
    public decimal DiscountAmount { get; set; }
    public required string TransactionId { get; set; }
    public required string GatewayResponse { get; set; }
    public PaymentStatus PaymentStatus { get; set; }
    public DateTime PaidDate { get; set; }
}
