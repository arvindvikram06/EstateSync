using System;

namespace EstateSync.Api.DTOs.Leases;

public record SubmitVacateNoticeRequest
{
    public DateTime RequestedExitDate { get; init; }
}
