using System;
using System.Threading;
using System.Threading.Tasks;
using EstateSync.Api.DTOs.Users;

namespace EstateSync.Api.Interfaces.Services.Users;

public interface IUserService
{
    Task<string> BecomeOwnerAsync(Guid userId, CancellationToken ct = default);
    Task<UserProfileResponse> GetProfileAsync(Guid userId, CancellationToken ct = default);
}
