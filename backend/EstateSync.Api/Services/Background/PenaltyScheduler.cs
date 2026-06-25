using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using EstateSync.Api.Models;
using EstateSync.Api.Repositories.Interfaces;

namespace EstateSync.Api.Services.Background;

public class PenaltyScheduler : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<PenaltyScheduler> _logger;

    public PenaltyScheduler(IServiceScopeFactory scopeFactory, ILogger<PenaltyScheduler> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("PenaltyScheduler Background Service started.");

        // Offset by 2 hours to avoid lock contention with other schedulers
        await Task.Delay(TimeSpan.FromHours(2), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await GeneratePenaltyBillsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while running PenaltyScheduler.");
            }

            await Task.Delay(TimeSpan.FromHours(24), stoppingToken);
        }
    }

    private async Task GeneratePenaltyBillsAsync(CancellationToken stoppingToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var leaseBillRepo = scope.ServiceProvider.GetRequiredService<ILeaseBillRepository>();
        var notificationRepo = scope.ServiceProvider.GetRequiredService<INotificationRepository>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var overdueBills = await leaseBillRepo.GetOverdueBillsWithLeaseAsync(DateTime.UtcNow);

        bool hasChanges = false;

        foreach (var bill in overdueBills)
        {
            var lease = bill.Lease;
            if (lease == null || lease.PenaltyPercentage == null || lease.PenaltyPercentage == 0)
                continue;

            // Skip if a penalty bill already exists for this lease + same due date
            bool penaltyExists = lease.LeaseBills != null &&
                System.Linq.Enumerable.Any(lease.LeaseBills, b =>
                    b.BillType == LeaseBillType.Penalty &&
                    b.DueDate.Date == bill.DueDate.Date);

            if (penaltyExists)
                continue;

            var penaltyAmount = Math.Round(bill.Amount * (lease.PenaltyPercentage.Value / 100m), 2);

            await leaseBillRepo.AddAsync(new LeaseBill
            {
                LeaseBillId = Guid.NewGuid(),
                LeaseId = lease.LeaseId,
                BillType = LeaseBillType.Penalty,
                DueDate = bill.DueDate,
                Amount = penaltyAmount,
                PendingAmount = penaltyAmount,
                CreatedAt = DateTime.UtcNow
            });

            // Notify tenant that a penalty bill has been generated
            await notificationRepo.AddAsync(new Notification
            {
                NotificationId = Guid.NewGuid(),
                UserId = lease.TenantId,
                Type = NotificationType.RentDue,
                Title = "Late Payment Penalty",
                Message = $"A penalty of ₹{penaltyAmount:N0} has been added to your account for overdue rent due on {bill.DueDate:dd MMM yyyy}.",
                EntityId = lease.LeaseId.ToString(),
                EntityType = "Lease",
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            });

            hasChanges = true;
            _logger.LogInformation("PenaltyScheduler: Generated penalty bill of ₹{Amount} for Lease {LeaseId} (DueDate {DueDate})",
                penaltyAmount, lease.LeaseId, bill.DueDate);
        }

        if (hasChanges)
        {
            await unitOfWork.SaveChangesAsync(stoppingToken);
        }
    }
}
