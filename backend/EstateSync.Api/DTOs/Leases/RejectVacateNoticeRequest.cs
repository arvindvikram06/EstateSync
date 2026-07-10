namespace EstateSync.Api.DTOs.Leases;

public record RejectVacateNoticeRequest
{
    public string Reason { get; init; } = string.Empty;
}
