using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using EstateSync.Api.Models;
using EstateSync.Api.Repositories.Interfaces;

namespace EstateSync.Api.Services.Background;

public class LeaseBillScheduler : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<LeaseBillScheduler> _logger;

    public LeaseBillScheduler(IServiceScopeFactory scopeFactory, ILogger<LeaseBillScheduler> _logger)
    {
        _scopeFactory = scopeFactory;
        this._logger = _logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("LeaseBill Background Generation Service started.");

        // Run once every day. We check every 24 hours.
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await GenerateLeaseBillsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while running LeaseBill generation background job.");
            }

            // Sleep for 24 hours (or a shorter interval for testing/dev environment if configured)
            await Task.Delay(TimeSpan.FromHours(24), stoppingToken);
        }
    }

    private async Task GenerateLeaseBillsAsync(CancellationToken stoppingToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var leaseRepo = scope.ServiceProvider.GetRequiredService<ILeaseRepository>();
        var leaseBillRepo = scope.ServiceProvider.GetRequiredService<ILeaseBillRepository>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var leases = await leaseRepo.GetActiveLeasesWithBillsAsync();
        var targetDate = DateTime.SpecifyKind(DateTime.UtcNow.Date, DateTimeKind.Utc);

        bool hasChanges = false;

        foreach (var lease in leases)
        {
            int multiplier = lease.PaymentFrequency switch
            {
                PaymentFrequency.Monthly => 1,
                PaymentFrequency.Quarterly => 3,
                PaymentFrequency.HalfYearly => 6,
                PaymentFrequency.Yearly => 12,
                _ => 1
            };

            DateTime currentDueDate = DateTime.SpecifyKind(lease.StartDate.Date, DateTimeKind.Utc);

            // Generate bills for all due dates up to today, within the lease terms
            while (currentDueDate <= targetDate && currentDueDate <= lease.EndDate.Date)
            {
                // Calculate difference in months between StartDate and the currentDueDate
                int monthsElapsed = ((currentDueDate.Year - lease.StartDate.Year) * 12) + currentDueDate.Month - lease.StartDate.Month;

                // Validate that the month elapsed matches the payment frequency options
                if (monthsElapsed % multiplier == 0)
                {
                    bool billExists = lease.LeaseBills.Any(b => 
                        b.DueDate.Date == currentDueDate && 
                        b.BillType == LeaseBillType.Rent);

                    if (!billExists)
                    {
                        var bill = new LeaseBill
                        {
                            LeaseBillId = Guid.NewGuid(),
                            LeaseId = lease.LeaseId,
                            BillType = LeaseBillType.Rent,
                            DueDate = currentDueDate,
                            Amount = lease.MonthlyRent * multiplier,
                            PendingAmount = lease.MonthlyRent * multiplier,
                            CreatedAt = DateTime.UtcNow
                        };

                        await leaseBillRepo.AddAsync(bill);
                        hasChanges = true;
                        _logger.LogInformation("Background Scheduler: Generated LeaseBill for Lease {LeaseId} ({Frequency}) with DueDate {DueDate}", 
                            lease.LeaseId, lease.PaymentFrequency, currentDueDate);
                    }
                }

                currentDueDate = currentDueDate.AddMonths(1);
            }
        }

        if (hasChanges)
        {
            await unitOfWork.SaveChangesAsync(stoppingToken);
        }
    }
}
