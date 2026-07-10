using System;

namespace EstateSync.Api.DTOs.Users;

public record UserProfileResponse
{
    public string FullName { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string PhoneNumber { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
    public bool IsKycVerified { get; init; }
    public bool IsOwnerVerified { get; init; }
    public string? OwnerType { get; init; }
    public string? CompanyName { get; init; }
    public string? CompanyType { get; init; }
    public string? GSTIN { get; init; }
}
