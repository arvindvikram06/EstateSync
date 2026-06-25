using System.Threading.Tasks;
using EstateSync.Api.DTOs.Auth;

namespace EstateSync.Api.Interfaces.Services.Auth;

public interface IAuthService
{
    Task<(AuthResponse Auth, string RefreshToken)> RegisterAsync(RegisterRequest request);
    Task<(AuthResponse Auth, string RefreshToken)> LoginAsync(LoginRequest request);
    Task<(AuthResponse Auth, string RefreshToken)> RefreshTokenAsync(string refreshToken);
    Task ForgotPasswordAsync(ForgotPasswordRequest request);
    Task ResetPasswordAsync(ResetPasswordRequest request);
}
