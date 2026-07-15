using EstateSync.Api.Interfaces.Services.Notifications;
using EstateSync.Api.Models;
using EstateSync.Api.Repositories.Interfaces;
using Microsoft.Extensions.Logging;

namespace EstateSync.Api.Services.Background;

public class LeaseExpiryJob
{
    private readonly ILeaseRepository _leaseRepo;
    private readonly INotificationSender _notif;
    private readonly IUnitOfWork _uow;
    private readonly ILogger<LeaseExpiryJob> _logger;

    public LeaseExpiryJob(ILeaseRepository leaseRepo, INotificationSender notif, IUnitOfWork uow, ILogger<LeaseExpiryJob> logger)
    {
        _leaseRepo = leaseRepo; _notif = notif; _uow = uow; _logger = logger;
    }

    public async Task RunAsync()
    {
        var expiredLeases = await _leaseRepo.GetActiveLeasesPastEndDateAsync(DateTime.UtcNow);
        bool hasChanges = false;

        foreach (var lease in expiredLeases)
        {
            lease.Status = LeaseStatus.Expired;
            hasChanges = true;

            if (lease.RentalApplication != null && lease.RentalApplication.Status == ApplicationStatus.Approved)
                lease.RentalApplication.Status = ApplicationStatus.Completed;

            if (lease.Property != null &&
                (lease.Property.Status == PropertyStatus.AvailableSoon || lease.Property.Status == PropertyStatus.Leased))
            {
                lease.Property.Status = PropertyStatus.Active;
            }

            _logger.LogInformation("LeaseExpiryJob: Expired Lease {LeaseId} for Property {PropertyId}", lease.LeaseId, lease.PropertyId);

            await _notif.SendAsync(new Notification
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
                await _notif.SendAsync(new Notification
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
                    await _notif.SendAsync(new Notification
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
            await _uow.SaveChangesAsync();
    }
}
