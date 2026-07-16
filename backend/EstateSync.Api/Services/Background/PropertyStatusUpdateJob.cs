using EstateSync.Api.Interfaces.Services.Notifications;
using EstateSync.Api.Models;
using EstateSync.Api.Repositories.Interfaces;
using Microsoft.Extensions.Logging;

namespace EstateSync.Api.Services.Background;

public class PropertyStatusUpdateJob
{
    private readonly ILeaseRepository _leaseRepo;
    private readonly INotificationSender _notif;
    private readonly IUnitOfWork _uow;
    private readonly ILogger<PropertyStatusUpdateJob> _logger;

    public PropertyStatusUpdateJob(ILeaseRepository leaseRepo, INotificationSender notif, IUnitOfWork uow, ILogger<PropertyStatusUpdateJob> logger)
    {
        _leaseRepo = leaseRepo; _notif = notif; _uow = uow; _logger = logger;
    }

    public async Task RunAsync()
    {
        var targetDate = DateTime.UtcNow.AddMonths(3);
        var leases = await _leaseRepo.GetActiveLeasesEndingBeforeAsync(targetDate);
        bool hasChanges = false;

        foreach (var lease in leases)
        {
            if (lease.Property == null || lease.Property.Status != PropertyStatus.Leased) continue;

            lease.Property.Status = PropertyStatus.AvailableSoon;
            hasChanges = true;

            _logger.LogInformation("PropertyStatusUpdateJob: Updated Property {PropertyId} to AvailableSoon (lease ends {EndDate})",
                lease.PropertyId, lease.EndDate);

            await _notif.SendAsync(new Notification
            {
                NotificationId = Guid.NewGuid(),
                UserId = lease.TenantId,
                Type = NotificationType.LeaseExpiring,
                Title = "Your Lease Is Ending Soon",
                Message = $"Your lease for '{lease.Property.Title}' ends on {lease.EndDate:dd MMM yyyy}. Please renew or prepare to vacate.",
                EntityId = lease.LeaseId.ToString(),
                EntityType = "Lease",
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            });

            await _notif.SendAsync(new Notification
            {
                NotificationId = Guid.NewGuid(),
                UserId = lease.Property.OwnerId,
                Type = NotificationType.LeaseExpiring,
                Title = "Property Will Be Available Soon",
                Message = $"The lease for '{lease.Property.Title}' ends on {lease.EndDate:dd MMM yyyy}. The property has been marked as Available Soon.",
                EntityId = lease.PropertyId.ToString(),
                EntityType = "Property",
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            });
        }

        if (hasChanges)
            await _uow.SaveChangesAsync();
    }
}
