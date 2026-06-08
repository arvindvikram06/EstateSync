namespace EstateSync.Api.DTOs.RentalApplications;

public record ReviewRentalApplicationRequest
{
    public required bool Approve { get; init; }
    public string Remarks { get; init; } = string.Empty;
}
