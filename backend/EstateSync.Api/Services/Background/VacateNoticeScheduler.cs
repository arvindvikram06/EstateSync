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

public class VacateNoticeScheduler : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<VacateNoticeScheduler> _logger;

    public VacateNoticeScheduler(IServiceScopeFactory scopeFactory, ILogger<VacateNoticeScheduler> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("VacateNoticeScheduler started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessDueNoticesAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in VacateNoticeScheduler.");
            }

            await Task.Delay(TimeSpan.FromHours(24), stoppingToken);
        }
    }

    private async Task ProcessDueNoticesAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var vacateRepo         = scope.ServiceProvider.GetRequiredService<ILeaseVacateNoticeRepository>();
        var notificationSender = scope.ServiceProvider.GetRequiredService<INotificationSender>();
        var unitOfWork         = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var dueNotices = await vacateRepo.GetDueForTerminationAsync(DateTime.UtcNow);

        bool hasChanges = false;

        foreach (var notice in dueNotices)
        {
            var lease    = notice.Lease;
            var property = lease?.Property;

            if (lease == null || property == null) continue;

            notice.Status            = VacateNoticeStatus.Accepted;
            lease.Status             = LeaseStatus.Terminated;
            lease.CancelledAt        = DateTime.UtcNow;
            lease.CancelledBy        = null;
            lease.CancellationReason = $"Vacate notice processed. Exit date: {notice.RequestedExitDate:dd MMM yyyy}.";
            lease.IsDepositForfeited = notice.PenaltyApplied;

            if (property.Status == PropertyStatus.AvailableSoon || property.Status == PropertyStatus.Leased)
                property.Status = PropertyStatus.Active;

            if (lease.RentalApplication != null && lease.RentalApplication.Status == ApplicationStatus.Approved)
                lease.RentalApplication.Status = ApplicationStatus.Completed;

            hasChanges = true;

            _logger.LogInformation(
                "VacateNoticeScheduler: Terminated Lease {LeaseId} (notice {NoticeId})",
                lease.LeaseId, notice.VacateNoticeId);

            await notificationSender.SendAsync(new Notification
            {
                NotificationId = Guid.NewGuid(),
                UserId         = lease.TenantId,
                Type           = NotificationType.LeaseTerminated,
                Title          = "Your Lease Has Been Terminated",
                Message        = $"Your lease for '{property.Title}' has ended as of {notice.RequestedExitDate:dd MMM yyyy}.",
                EntityId       = lease.LeaseId.ToString(),
                EntityType     = "Lease",
                IsRead         = false,
                CreatedAt      = DateTime.UtcNow
            });

            await notificationSender.SendAsync(new Notification
            {
                NotificationId = Guid.NewGuid(),
                UserId         = property.OwnerId,
                Type           = NotificationType.LeaseTerminated,
                Title          = "Vacate Notice Processed — Property Now Active",
                Message        = $"The tenant's vacate notice for '{property.Title}' has been processed. The property is now available for new tenants.",
                EntityId       = property.PropertyId.ToString(),
                EntityType     = "Property",
                IsRead         = false,
                CreatedAt      = DateTime.UtcNow
            });
        }

        if (hasChanges)
            await unitOfWork.SaveChangesAsync(ct);
    }
}
