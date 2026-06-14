namespace EstateSync.Api.DTOs.OwnerVerification;

public record SubmitOwnerVerificationRequest
{
    public required string PanOrTaxId { get; init; }
}
