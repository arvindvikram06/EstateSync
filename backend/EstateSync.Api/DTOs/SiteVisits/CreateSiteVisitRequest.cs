using System;

namespace EstateSync.Api.DTOs.SiteVisits;

public class CreateSiteVisitRequest
{
    public Guid PropertyId { get; set; }
    public Guid SlotId { get; set; }
}
