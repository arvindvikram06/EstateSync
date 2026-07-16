using System;

namespace EstateSync.Api.DTOs.SiteVisits;

public class CreateSiteVisitRequest
{
    public Guid PropertyId { get; set; }
    public DateTime VisitDate { get; set; } // Stored in UTC (date portion)
    public TimeOnly VisitTime { get; set; } // Exact time of visit
}
