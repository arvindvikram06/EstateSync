namespace EstateSync.Api.DTOs.Auth;

public record VerifyEmailRequest
{
    public string Token { get; init; } = string.Empty;
}
