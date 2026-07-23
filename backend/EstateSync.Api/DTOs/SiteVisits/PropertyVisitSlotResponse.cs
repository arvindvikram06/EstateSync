namespace EstateSync.Api.DTOs.SiteVisits;

public sealed record PropertyVisitSlotResponse(
    Guid SlotId,
    Guid PropertyId,
    DateTime Date,
    string StartTime,
    string EndTime,
    int DurationMinutes,
    string Status);
