using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using EstateSync.Api.Models;
using EstateSync.Api.Repositories.Interfaces;

namespace EstateSync.Api.Services.Background;

public class PropertyStatusUpdateScheduler : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<PropertyStatusUpdateScheduler> _logger;

    public PropertyStatusUpdateScheduler(IServiceScopeFactory scopeFactory, ILogger<PropertyStatusUpdateScheduler> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("PropertyStatusUpdateScheduler Background Service started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await UpdatePropertyStatusesAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while running PropertyStatusUpdateScheduler background job.");
            }

            // Sleep for 24 hours
            await Task.Delay(TimeSpan.FromHours(24), stoppingToken);
        }
    }

    private async Task UpdatePropertyStatusesAsync(CancellationToken stoppingToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var leaseRepo = scope.ServiceProvider.GetRequiredService<ILeaseRepository>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var targetDate = DateTime.UtcNow.AddMonths(3);
        var leases = await leaseRepo.GetActiveLeasesEndingBeforeAsync(targetDate);

        bool hasChanges = false;

        foreach (var lease in leases)
        {
            if (lease.Property != null && lease.Property.Status == PropertyStatus.Leased)
            {
                lease.Property.Status = PropertyStatus.AvailableSoon;
                hasChanges = true;
                _logger.LogInformation("Background Scheduler: Updated Property {PropertyId} status to AvailableSoon because active lease ends on {EndDate}", 
                lease.PropertyId, lease.EndDate);
            }
        }

        if (hasChanges)
        {
            await unitOfWork.SaveChangesAsync(stoppingToken);
        }
    }
}
