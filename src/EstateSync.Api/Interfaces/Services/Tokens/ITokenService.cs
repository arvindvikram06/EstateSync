using System.Security.Claims;
using EstateSync.Api.Models;

namespace EstateSync.Api.Interfaces.Services.Tokens;

public interface ITokenService
{
    string GenerateAccessToken(User user);
    string GenerateRefreshToken();
    ClaimsPrincipal? GetPrincipalFromToken(string token);
    ClaimsPrincipal? GetPrincipalFromExpiredToken(string token);
}
