namespace EstateSync.Api.DTOs.Leases;

public record SignLeaseRequest
{
    public required string SignatureText { get; init; }
    public required string SignatureImageBase64 { get; init; }
}
