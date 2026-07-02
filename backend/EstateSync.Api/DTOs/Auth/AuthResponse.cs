using System.Collections.Generic;

namespace EstateSync.Api.DTOs.Auth;

public record AuthResponse
{
    public required Guid UserId { get; init; }
    public required string FullName { get; init; }
    public required string AccessToken { get; init; }
    public required string Email { get; init; }
    public required IEnumerable<string> Roles { get; init; }
    public bool IsKycVerified { get; init; }
    public bool IsOwnerVerified { get; init; }
}
