using System;

namespace EstateSync.Api.DTOs.Leases;

public record ApproveRenewalRequest
{
    public DateTime NewEndDate { get; init; }
}
