using System;
using EstateSync.Api.Models;

namespace EstateSync.Api.DTOs.Leases;

public record LeaseBillDetailResponse
{
    public Guid LeaseBillId { get; init; }
    public LeaseBillType BillType { get; init; }
    public DateTime DueDate { get; init; }
    public decimal Amount { get; init; }
    public decimal PendingAmount { get; init; }
    public string Status => PendingAmount <= 0 ? "Paid" : "Pending";
}

public record PaymentRecordResponse
{
    public Guid PaymentId { get; init; }
    public Guid LeaseBillId { get; init; }
    public LeaseBillType BillType { get; init; }
    public decimal Amount { get; init; }
    public decimal PlatformFee { get; init; }
    public string TransactionId { get; init; } = string.Empty;
    public string PaymentStatus { get; init; } = string.Empty;
    public DateTime PaidDate { get; init; }
}
