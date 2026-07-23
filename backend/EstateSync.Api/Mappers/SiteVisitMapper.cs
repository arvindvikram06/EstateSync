using EstateSync.Api.DTOs.SiteVisits;
using EstateSync.Api.Models;

namespace EstateSync.Api.Mappers;

public class SiteVisitMapper
{
    public SiteVisitResponse MapToResponse(SiteVisit visit)
    {
        return new SiteVisitResponse
        {
            SiteVisitId = visit.SiteVisitId,
            PropertyId = visit.PropertyId,
            PropertyTitle = visit.Property?.Title,
            TenantId = visit.TenantId,
            TenantName = visit.Tenant?.FullName,
            SlotId = visit.PropertyVisitSlotId,
            PreviousSlotId = visit.PreviousPropertyVisitSlotId,
            VisitDate = visit.VisitDate,
            VisitTime = visit.VisitTime.ToString("HH:mm"),
            Status = visit.Status.ToString(),
            OwnerNotes = visit.OwnerNotes,
            RescheduledByUserId = visit.RescheduledByUserId,
            RescheduledAt = visit.RescheduledAt,
            RescheduleReason = visit.RescheduleReason,
            DurationMinutes = 60,
            CreatedAt = visit.CreatedAt,
            UpdatedAt = visit.UpdatedAt
        };
    }
}
