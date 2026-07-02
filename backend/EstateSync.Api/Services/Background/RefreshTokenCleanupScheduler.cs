using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using EstateSync.Api.Repositories.Interfaces;

namespace EstateSync.Api.Services.Background;

/// <summary>
/// Periodically prunes refresh tokens that are expired or were revoked long ago, so the
/// refresh-token table does not grow unbounded. Recently-revoked rows are retained so the
/// rotation grace window in <see cref="EstateSync.Api.Services.Auth.AuthService"/> still works.
/// </summary>
public class RefreshTokenCleanupScheduler : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromHours(24);
    private static readonly TimeSpan RevokedRetention = TimeSpan.FromDays(2);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<RefreshTokenCleanupScheduler> _logger;

    public RefreshTokenCleanupScheduler(IServiceScopeFactory scopeFactory, ILogger<RefreshTokenCleanupScheduler> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("RefreshTokenCleanupScheduler Background Service started.");

        // Offset from the other schedulers to avoid startup contention.
        await Task.Delay(TimeSpan.FromMinutes(30), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CleanupAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while running RefreshTokenCleanupScheduler background job.");
            }

            await Task.Delay(Interval, stoppingToken);
        }
    }

    private async Task CleanupAsync()
    {
        using var scope = _scopeFactory.CreateScope();
        var refreshTokenRepo = scope.ServiceProvider.GetRequiredService<IRefreshTokenRepository>();

        var deleted = await refreshTokenRepo.DeleteExpiredAndStaleRevokedAsync(DateTime.UtcNow, RevokedRetention);

        if (deleted > 0)
        {
            _logger.LogInformation("RefreshTokenCleanupScheduler: removed {Count} expired/stale refresh tokens.", deleted);
        }
    }
}
