using System.Threading.Tasks;
using EstateSync.Api.DTOs.Auth;

namespace EstateSync.Api.Interfaces.Services.Auth;

public interface IAuthService
{
    Task RegisterAsync(RegisterRequest request);
    Task VerifyEmailAsync(string token);
    Task ResendVerificationAsync(string email);
    Task<(AuthResponse Auth, string RefreshToken)> LoginAsync(LoginRequest request);
    Task<(AuthResponse Auth, string RefreshToken)> RefreshTokenAsync(string refreshToken);
    Task LogoutAsync(string? refreshToken);
    Task ForgotPasswordAsync(ForgotPasswordRequest request);
    Task ResetPasswordAsync(ResetPasswordRequest request);
}
