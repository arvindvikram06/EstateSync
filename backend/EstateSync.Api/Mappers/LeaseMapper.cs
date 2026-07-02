using System;
using EstateSync.Api.DTOs.Leases;
using EstateSync.Api.Models;

namespace EstateSync.Api.Mappers;

public class LeaseMapper
{
    public LeaseResponse MapToResponse(Lease lease)
    {
        return new LeaseResponse
        {
            LeaseId = lease.LeaseId,
            PropertyId = lease.PropertyId,
            PropertyTitle = lease.Property?.Title,
            PropertyAddress = lease.Property?.AddressLine,
            TenantId = lease.TenantId,
            TenantName = lease.Tenant?.FullName,
            TenantEmail = lease.Tenant?.Email,
            OwnerId = lease.Property?.OwnerId,
            OwnerName = lease.Property?.Owner?.FullName,
            ApplicationId = lease.ApplicationId,
            StartDate = lease.StartDate,
            EndDate = lease.EndDate,
            MonthlyRent = lease.MonthlyRent,
            SecurityDeposit = lease.SecurityDeposit,
            PaymentFrequency = lease.PaymentFrequency.ToString(),
            NoticePeriodDays = lease.NoticePeriodDays,
            PenaltyPercentage = lease.PenaltyPercentage,
            AgreementText = lease.AgreementText,
            AgreementPdfUrl = lease.SignedAgreementUrl,
            TenantSignedAt = lease.TenantSignedAt,
            TenantSignature = lease.TenantSignature,
            TenantSignatureImageUrl = lease.TenantSignatureImageUrl,
            OwnerSignedAt = lease.OwnerSignedAt,
            OwnerSignature = lease.OwnerSignature,
            OwnerSignatureImageUrl = lease.OwnerSignatureImageUrl,
            SignatureVerificationHash = lease.SignatureVerificationHash,
            Status = lease.Status.ToString(),
            ActivatedAt = lease.ActivatedAt,
            AdminApprovedById = lease.AdminApprovedById,
            AdminApprovedAt = lease.AdminApprovedAt,
            AdminRemarks = lease.AdminRemarks,
            CancelledBy = lease.CancelledBy,
            CancellationReason = lease.CancellationReason,
            CancelledAt = lease.CancelledAt
        };
    }
}
