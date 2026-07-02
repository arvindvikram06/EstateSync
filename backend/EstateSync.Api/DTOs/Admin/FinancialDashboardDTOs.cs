using System;
using System.Collections.Generic;

namespace EstateSync.Api.DTOs.Admin;

public record OwnerSettlementResponse(
    Guid OwnerSettlementId,
    Guid OwnerId,
    string OwnerName,
    string OwnerEmail,
    string StripePayoutId,
    decimal Amount,
    string Currency,
    string DestinationId,
    DateTime ArrivalDate,
    string Status,
    DateTime CreatedAt
);

public record SettlementSummaryResponse(
    decimal TotalPaid,
    decimal TotalPending,
    decimal TotalFailed,
    int TotalSettlements,
    decimal TotalPlatformFeesCollected,
    decimal TotalPaymentsVolume
);

public record ConnectedAccountResponse(
    Guid Id,
    Guid OwnerId,
    string OwnerName,
    string OwnerEmail,
    string StripeAccountId,
    bool ChargesEnabled,
    bool PayoutsEnabled,
    bool DetailsSubmitted,
    DateTime? LastSyncedAt,
    DateTime CreatedAt
);

public record AdminPaymentResponse(
    Guid PaymentId,
    Guid LeaseBillId,
    Guid PaidByUserId,
    string PaidByName,
    string PaidByEmail,
    decimal Amount,
    decimal PlatformFee,
    string TransactionId,
    string PaymentStatus,
    DateTime PaidDate,
    DateTime CreatedAt
);
