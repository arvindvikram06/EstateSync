using System;
using EstateSync.Api.Models;

namespace EstateSync.Api.DTOs.Leases;

public record LeaseBillResponse
{
    public Guid LeaseBillId { get; init; }
    public Guid LeaseId { get; init; }
    public string PropertyTitle { get; init; } = string.Empty;
    public LeaseBillType BillType { get; init; }
    public DateTime DueDate { get; init; }
    public decimal Amount { get; init; }
    public decimal PendingAmount { get; init; }
    public string Status => PendingAmount <= 0 ? "Paid" : "Pending";
}
