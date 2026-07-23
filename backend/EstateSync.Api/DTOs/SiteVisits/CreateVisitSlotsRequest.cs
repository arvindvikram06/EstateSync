namespace EstateSync.Api.DTOs.SiteVisits;

public sealed class CreateVisitSlotsRequest
{
    public DateTime Date { get; init; }
    public IReadOnlyCollection<TimeOnly> StartTimes { get; init; } = [];
}
