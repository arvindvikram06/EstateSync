using System;

namespace EstateSync.Api.DTOs.Leases;

public record LeaseResponse
{
    public Guid LeaseId { get; init; }
    public Guid PropertyId { get; init; }
    public string? PropertyTitle { get; init; }
    public Guid TenantId { get; init; }
    public string? TenantName { get; init; }
    public Guid ApplicationId { get; init; }
    public DateTime StartDate { get; init; }
    public DateTime EndDate { get; init; }
    public decimal MonthlyRent { get; init; }
    public decimal SecurityDeposit { get; init; }
    public string PaymentFrequency { get; init; } = string.Empty;
    public int? NoticePeriodDays { get; init; }
    public decimal? PenaltyPercentage { get; init; }
    public string? AgreementText { get; init; }
    public string? AgreementPdfUrl { get; init; }
    public string? TenantSignatureImageUrl { get; init; }
    public string? OwnerSignatureImageUrl { get; init; }
    public string? SignatureVerificationHash { get; init; }
    public Guid? AdminApprovedById { get; init; }
    public DateTime? AdminApprovedAt { get; init; }
    public string? AdminRemarks { get; init; }
    public DateTime? TenantSignedAt { get; init; }
    public string? TenantSignature { get; init; }
    public DateTime? OwnerSignedAt { get; init; }
    public string? OwnerSignature { get; init; }
    public string Status { get; init; } = string.Empty;
    public DateTime? ActivatedAt { get; init; }
    public Guid? CancelledBy { get; init; }
    public string? CancellationReason { get; init; }
    public DateTime? CancelledAt { get; init; }
}
