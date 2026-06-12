namespace EstateSync.Api.DTOs.TenantVerification;

public record ReviewTenantVerificationRequest
{
    public required bool Approve { get; init; }
    public string? Remarks { get; init; }
}
