namespace EstateSync.Api.DTOs.Auth;

public record ResetPasswordRequest
{
    public required string Email { get; init; }
    public required string Token { get; init; }
    public required string NewPassword { get; init; }
}
