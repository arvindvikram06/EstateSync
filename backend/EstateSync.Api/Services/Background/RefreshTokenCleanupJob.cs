using EstateSync.Api.Repositories.Interfaces;
using Microsoft.Extensions.Logging;

namespace EstateSync.Api.Services.Background;

public class RefreshTokenCleanupJob
{
    private static readonly TimeSpan RevokedRetention = TimeSpan.FromDays(2);

    private readonly IRefreshTokenRepository _refreshTokenRepo;
    private readonly ILogger<RefreshTokenCleanupJob> _logger;

    public RefreshTokenCleanupJob(IRefreshTokenRepository refreshTokenRepo, ILogger<RefreshTokenCleanupJob> logger)
    {
        _refreshTokenRepo = refreshTokenRepo; _logger = logger;
    }

    public async Task RunAsync()
    {
        var deleted = await _refreshTokenRepo.DeleteExpiredAndStaleRevokedAsync(DateTime.UtcNow, RevokedRetention);

        if (deleted > 0)
            _logger.LogInformation("RefreshTokenCleanupJob: Removed {Count} expired/stale refresh tokens.", deleted);
    }
}
