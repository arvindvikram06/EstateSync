namespace EstateSync.Api.DTOs.OwnerVerification;

public record SubmitOwnerVerificationRequest
{
    public required string BankAccountNumber { get; init; }
    public required string BankIFSCCode { get; init; }
    public required string BankAccountHolderName { get; init; }
    public required string BankName { get; init; }
}
