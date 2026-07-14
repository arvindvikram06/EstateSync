using System;
using System.Threading.Tasks;
using EstateSync.Api.Interfaces.Services.Auth;
using StackExchange.Redis;

namespace EstateSync.Api.Services.Auth;

public class RedisEmailVerificationTokenStore : IEmailVerificationTokenStore
{
    private const string KeyPrefix = "emailverify:";
    private readonly IConnectionMultiplexer _redis;

    public RedisEmailVerificationTokenStore(IConnectionMultiplexer redis)
    {
        _redis = redis ?? throw new ArgumentNullException(nameof(redis));
    }

    public async Task StoreAsync(string token, Guid userId, TimeSpan ttl)
    {
        var db = _redis.GetDatabase();
        await db.StringSetAsync(KeyPrefix + token, userId.ToString(), ttl);
    }

    public async Task<Guid?> ConsumeAsync(string token)
    {
        var db = _redis.GetDatabase();
        var value = await db.StringGetDeleteAsync(KeyPrefix + token);
        if (value.IsNullOrEmpty) return null;

        return Guid.TryParse(value.ToString(), out var userId) ? userId : null;
    }
}
