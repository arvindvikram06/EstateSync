using System;

namespace EstateSync.Api.DTOs.IdentityVerification;

public record IdentityVerificationResponse : BaseVerificationResponse
{
    public Guid UserId { get; init; }
    public required string FullName { get; init; }
    public required string Email { get; init; }
    public string? AadhaarNumber { get; init; }
    public string? PanNumber { get; init; }
}
