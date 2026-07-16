using EstateSync.Api.Models;

namespace EstateSync.Api.DTOs.SiteVisits;

public class UpdateSiteVisitStatusRequest
{
    public SiteVisitStatus Status { get; set; }
    public string? OwnerNotes { get; set; }
}
