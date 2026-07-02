namespace EstateSync.Api.DTOs.Users;

public record UserProfileResponse
{
    public bool IsKycVerified { get; init; }
    public bool IsOwnerVerified { get; init; }
}
