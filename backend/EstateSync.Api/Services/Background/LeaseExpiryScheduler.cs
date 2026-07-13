using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using EstateSync.Api.Interfaces.Services.Notifications;
using EstateSync.Api.Models;
using EstateSync.Api.Repositories.Interfaces;

namespace EstateSync.Api.Services.Background;

public class LeaseExpiryScheduler : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<LeaseExpiryScheduler> _logger;

    public LeaseExpiryScheduler(IServiceScopeFactory scopeFactory, ILogger<LeaseExpiryScheduler> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("LeaseExpiryScheduler Background Service started.");

        // Offset by 1 hour to avoid lock contention with PropertyStatusUpdateScheduler
        await Task.Delay(TimeSpan.FromHours(1), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ExpireLeasesAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while running LeaseExpiryScheduler background job.");
            }

            await Task.Delay(TimeSpan.FromHours(24), stoppingToken);
        }
    }

    private async Task ExpireLeasesAsync(CancellationToken stoppingToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var leaseRepo = scope.ServiceProvider.GetRequiredService<ILeaseRepository>();
        var notificationSender = scope.ServiceProvider.GetRequiredService<INotificationSender>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var expiredLeases = await leaseRepo.GetActiveLeasesPastEndDateAsync(DateTime.UtcNow);

        bool hasChanges = false;

        foreach (var lease in expiredLeases)
        {
            lease.Status = LeaseStatus.Expired;
            hasChanges = true;

            // Close out the application so it cannot be used to create another lease.
            if (lease.RentalApplication != null && lease.RentalApplication.Status == ApplicationStatus.Approved)
                lease.RentalApplication.Status = ApplicationStatus.Completed;

            if (lease.Property != null &&
                (lease.Property.Status == PropertyStatus.AvailableSoon || lease.Property.Status == PropertyStatus.Leased))
            {
                lease.Property.Status = PropertyStatus.Active;
            }

            _logger.LogInformation("LeaseExpiryScheduler: Expired Lease {LeaseId} for Property {PropertyId}",
                lease.LeaseId, lease.PropertyId);

            await notificationSender.SendAsync(new Notification
            {
                NotificationId = Guid.NewGuid(),
                UserId = lease.TenantId,
                Type = NotificationType.LeaseExpired,
                Title = "Your Lease Has Expired",
                Message = $"Your lease for '{lease.Property?.Title}' has expired as of {lease.EndDate:dd MMM yyyy}.",
                EntityId = lease.LeaseId.ToString(),
                EntityType = "Lease",
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            });

            if (lease.Property != null)
            {
                await notificationSender.SendAsync(new Notification
                {
                    NotificationId = Guid.NewGuid(),
                    UserId = lease.Property.OwnerId,
                    Type = NotificationType.LeaseExpired,
                    Title = "Lease Expired — Property Now Active",
                    Message = $"The lease for '{lease.Property.Title}' expired on {lease.EndDate:dd MMM yyyy}. The property is now available for new tenants.",
                    EntityId = lease.PropertyId.ToString(),
                    EntityType = "Property",
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow
                });

                if (!lease.IsDepositForfeited)
                {
                    await notificationSender.SendAsync(new Notification
                    {
                        NotificationId = Guid.NewGuid(),
                        UserId = lease.Property.OwnerId,
                        Type = NotificationType.DepositReturnReminder,
                        Title = "Security Deposit Return Reminder",
                        Message = $"Please return the security deposit of ₹{lease.SecurityDeposit:N0} to your tenant for '{lease.Property.Title}' within 7 days.",
                        EntityId = lease.LeaseId.ToString(),
                        EntityType = "Lease",
                        IsRead = false,
                        CreatedAt = DateTime.UtcNow
                    });
                }
            }
        }

        if (hasChanges)
        {
            await unitOfWork.SaveChangesAsync(stoppingToken);
        }
    }
}
