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
            TenantId = lease.TenantId,
            TenantName = lease.Tenant?.FullName,
            ApplicationId = lease.ApplicationId,
            StartDate = lease.StartDate,
            EndDate = lease.EndDate,
            MonthlyRent = lease.MonthlyRent,
            SecurityDeposit = lease.SecurityDeposit,
            PaymentFrequency = lease.PaymentFrequency.ToString(),
            NoticePeriodDays = lease.NoticePeriodDays,
            PenaltyPercentage = lease.PenaltyPercentage,
            AgreementText = lease.AgreementText,
            UnsignedAgreementUrl = lease.UnsignedAgreementUrl,
            SignedAgreementUrl = lease.SignedAgreementUrl,
            TenantSignedAt = lease.TenantSignedAt,
            TenantSignature = lease.TenantSignature,
            OwnerSignedAt = lease.OwnerSignedAt,
            OwnerSignature = lease.OwnerSignature,
            Status = lease.Status.ToString(),
            ActivatedAt = lease.ActivatedAt,
            CancelledBy = lease.CancelledBy,
            CancellationReason = lease.CancellationReason,
            CancelledAt = lease.CancelledAt
        };
    }
}
