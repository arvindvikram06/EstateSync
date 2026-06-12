namespace EstateSync.Api.DTOs.IdentityVerification;

public record SubmitIdentityVerificationRequest
{
    public required string AadhaarNumber { get; init; }
    public required string PanNumber { get; init; }
}
