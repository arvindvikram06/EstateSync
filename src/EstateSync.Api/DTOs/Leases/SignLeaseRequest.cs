using Microsoft.AspNetCore.Http;

namespace EstateSync.Api.DTOs.Leases;

public record SignLeaseRequest
{
    public required string SignatureText { get; init; }
    public IFormFile? SignedAgreementFile { get; init; }
}
