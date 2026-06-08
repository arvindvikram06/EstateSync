namespace EstateSync.Api.DTOs.Auth;

public record ForgotPasswordRequest
{
    public required string Email { get; init; }
}
