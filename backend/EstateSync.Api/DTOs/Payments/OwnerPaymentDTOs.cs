using System;

namespace EstateSync.Api.DTOs.Payments;

public record OwnerEarningsSummaryResponse(
    decimal TotalPaid,
    decimal TotalPending,
    int TotalPayouts,
    DateTime? LastPayoutDate,
    decimal? LastPayoutAmount
);

public class OwnerAvailableBalanceResponse
{
    public decimal Available        { get; set; }   // ready for standard payout now
    public decimal InstantAvailable { get; set; }   // settled but still in T+2 window
    public decimal Pending          { get; set; }   // not yet cleared
    public string  Currency         { get; set; } = "INR";
    public bool    PayoutsEnabled   { get; set; }
}

public class RequestPayoutRequest
{
    public string Method { get; set; } = "standard"; // "standard" | "instant"
}

public class RequestPayoutResponse
{
    public string   StripePayoutId { get; set; } = string.Empty;
    public decimal  Amount         { get; set; }
    public string   Currency       { get; set; } = string.Empty;
    public DateTime ArrivalDate    { get; set; }
    public string   Status         { get; set; } = string.Empty;
    public string   Method         { get; set; } = string.Empty;
}
