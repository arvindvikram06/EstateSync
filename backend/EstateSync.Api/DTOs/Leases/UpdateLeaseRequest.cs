using System;
using EstateSync.Api.Models;

namespace EstateSync.Api.DTOs.Leases;

public record UpdateLeaseRequest
{
    public DateTime? StartDate { get; init; }
    public DateTime? EndDate { get; init; }
    public decimal? MonthlyRent { get; init; }
    public decimal? SecurityDeposit { get; init; }
    public PaymentFrequency? PaymentFrequency { get; init; }
    public int? NoticePeriodDays { get; init; }
    public decimal? PenaltyPercentage { get; init; }
    public string? AgreementText { get; init; }
}
