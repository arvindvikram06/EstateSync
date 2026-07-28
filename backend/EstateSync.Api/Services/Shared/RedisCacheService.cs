using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using EstateSync.Api.Interfaces.Services.Shared;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace EstateSync.Api.Services.Shared;

public sealed class RedisCacheService : ICacheService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly IDatabase _database;
    private readonly ILogger<RedisCacheService> _logger;

    public RedisCacheService(
        IConnectionMultiplexer redis,
        ILogger<RedisCacheService> logger)
    {
        _database = redis.GetDatabase();
        _logger = logger;
    }

    public async Task<T?> GetAsync<T>(string key, CancellationToken ct = default)
    {
        try
        {
            ct.ThrowIfCancellationRequested();
            var value = await _database.StringGetAsync(key);
            return value.HasValue
                ? JsonSerializer.Deserialize<T>(value.ToString(), JsonOptions)
                : default;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex) when (ex is RedisException or JsonException)
        {
            _logger.LogWarning(ex, "Cache read failed for {CacheKey}; falling back to the database", key);
            return default;
        }
    }

    public async Task SetAsync<T>(
        string key,
        T value,
        TimeSpan ttl,
        CancellationToken ct = default)
    {
        try
        {
            ct.ThrowIfCancellationRequested();
            var json = JsonSerializer.Serialize(value, JsonOptions);
            await _database.StringSetAsync(key, json, ttl);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex) when (ex is RedisException or JsonException)
        {
            _logger.LogWarning(ex, "Cache write failed for {CacheKey}; response will remain uncached", key);
        }
    }

    public async Task RemoveAsync(string key, CancellationToken ct = default)
    {
        try
        {
            ct.ThrowIfCancellationRequested();
            await _database.KeyDeleteAsync(key);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (RedisException ex)
        {
            _logger.LogWarning(ex, "Cache invalidation failed for {CacheKey}", key);
        }
    }

    public async Task<long> IncrementAsync(string key, CancellationToken ct = default)
    {
        try
        {
            ct.ThrowIfCancellationRequested();
            return await _database.StringIncrementAsync(key);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (RedisException ex)
        {
            _logger.LogWarning(ex, "Cache version increment failed for {CacheKey}", key);
            return 0;
        }
    }
}
