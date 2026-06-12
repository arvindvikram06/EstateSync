using System;
using System.Threading;
using System.Threading.Tasks;
using EstateSync.Api.Models;

namespace EstateSync.Api.Interfaces.Services.Auth;

public interface ICurrentUserService
{
    Guid? UserId { get; }
    bool IsAuthenticated { get; }
    Task<User> GetCurrentUserAsync(CancellationToken ct = default);
    Task<User> GetUserOrThrowAsync(Guid id, CancellationToken ct = default);
}
