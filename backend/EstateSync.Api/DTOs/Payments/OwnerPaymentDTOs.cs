using System;

namespace EstateSync.Api.DTOs.Payments;

public record OwnerEarningsSummaryResponse(
    decimal TotalPaid,
    decimal TotalPending,
    int TotalPayouts,
    DateTime? LastPayoutDate,
    decimal? LastPayoutAmount
);
