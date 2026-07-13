namespace EstateSync.Api.DTOs.Users;

public record UpdateProfileRequest
{
    public required string FullName { get; init; }
    public required string PhoneNumber { get; init; }
}
