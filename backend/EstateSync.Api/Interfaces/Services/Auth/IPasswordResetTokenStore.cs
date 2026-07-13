using System;
using System.Threading.Tasks;

namespace EstateSync.Api.Interfaces.Services.Auth;

public interface IPasswordResetTokenStore
{
    Task StoreAsync(string token, Guid userId, TimeSpan ttl);
    Task<Guid?> ConsumeAsync(string token);
}
