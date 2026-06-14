using System;

namespace EstateSync.Api.DTOs.OwnerVerification;

public record OwnerVerificationResponse : BaseVerificationResponse
{
    public Guid UserId { get; init; }
    public required string FullName { get; init; }
    public required string Email { get; init; }
    public string? PanOrTaxId { get; init; }
}
