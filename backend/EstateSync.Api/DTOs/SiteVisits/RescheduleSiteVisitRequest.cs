using System;

namespace EstateSync.Api.DTOs.SiteVisits;

public sealed class RescheduleSiteVisitRequest
{
    public Guid NewSlotId { get; init; }
    public string Reason { get; init; } = string.Empty;
}
