using System;
using EstateSync.Api.Models;

namespace EstateSync.Api.DTOs.Leases;

public record CreateLeaseRequest
{
    public required Guid ApplicationId { get; init; }
    public required DateTime StartDate { get; init; }
    public required DateTime EndDate { get; init; }
    public required decimal MonthlyRent { get; init; }
    public required decimal SecurityDeposit { get; init; }
    public required PaymentFrequency PaymentFrequency { get; init; }
    public int? NoticePeriodDays { get; init; }
    public decimal? PenaltyPercentage { get; init; }
    public string? AgreementText { get; init; }
}
