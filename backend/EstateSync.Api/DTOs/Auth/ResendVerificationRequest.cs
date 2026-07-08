namespace EstateSync.Api.DTOs.Auth;

public record ResendVerificationRequest
{
    public string Email { get; init; } = string.Empty;
}
