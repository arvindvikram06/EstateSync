using System;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using EstateSync.Api.Interfaces.Services.Auth;
using EstateSync.Api.Repositories.Interfaces;
using EstateSync.Api.Models;
using EstateSync.Api.Exceptions;

namespace EstateSync.Api.Services.Auth;

public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IUserRepository _userRepository;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor, IUserRepository userRepository)
    {
        _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
    }

    public Guid? UserId
    {
        get
        {
            var userIdStr = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdStr))
            {
                // Fallback to Sub claim commonly used in JWTs
                userIdStr = _httpContextAccessor.HttpContext?.User?.FindFirst("sub")?.Value;
            }
            return Guid.TryParse(userIdStr, out var parsedGuid) ? parsedGuid : null;
        }
    }

    public bool IsAuthenticated => _httpContextAccessor.HttpContext?.User?.Identity?.IsAuthenticated ?? false;

    public async Task<User> GetCurrentUserAsync(CancellationToken ct = default)
    {
        var id = UserId;
        if (id == null)
        {
            throw new UnauthorizedException("User is not authenticated.");
        }

        return await GetUserOrThrowAsync(id.Value, ct);
    }

    public async Task<User> GetUserOrThrowAsync(Guid id, CancellationToken ct = default)
    {
        var user = await _userRepository.GetByIdAsync(id);
        if (user == null)
        {
            throw new NotFoundException("User", id);
        }

        return user;
    }
}
