using System;
using System.Threading;
using System.Threading.Tasks;

namespace EstateSync.Api.Interfaces.Services.Users;

public interface IUserService
{
    Task<string> BecomeOwnerAsync(Guid userId, CancellationToken ct = default);
}
