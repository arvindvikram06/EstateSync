using EstateSync.Api.Interfaces.Services.Notifications;
using EstateSync.Api.Models;
using EstateSync.Api.Repositories.Interfaces;
using Microsoft.Extensions.Logging;

namespace EstateSync.Api.Services.Background;

public class VacateNoticeJob
{
    private readonly ILeaseVacateNoticeRepository _vacateRepo;
    private readonly INotificationSender _notif;
    private readonly IUnitOfWork _uow;
    private readonly ILogger<VacateNoticeJob> _logger;

    public VacateNoticeJob(ILeaseVacateNoticeRepository vacateRepo, INotificationSender notif, IUnitOfWork uow, ILogger<VacateNoticeJob> logger)
    {
        _vacateRepo = vacateRepo; _notif = notif; _uow = uow; _logger = logger;
    }

    public async Task RunAsync()
    {
        var dueNotices = await _vacateRepo.GetDueForTerminationAsync(DateTime.UtcNow);
        bool hasChanges = false;

        foreach (var notice in dueNotices)
        {
            var lease = notice.Lease;
            var property = lease?.Property;
            if (lease == null || property == null) continue;

            notice.Status = VacateNoticeStatus.Accepted;
            lease.Status = LeaseStatus.Terminated;
            lease.CancelledAt = DateTime.UtcNow;
            lease.CancelledBy = null;
            lease.CancellationReason = $"Vacate notice processed. Exit date: {notice.RequestedExitDate:dd MMM yyyy}.";
            lease.IsDepositForfeited = notice.PenaltyApplied;

            if (property.Status == PropertyStatus.AvailableSoon || property.Status == PropertyStatus.Leased)
                property.Status = PropertyStatus.Active;

            if (lease.RentalApplication != null && lease.RentalApplication.Status == ApplicationStatus.Approved)
                lease.RentalApplication.Status = ApplicationStatus.Completed;

            hasChanges = true;

            _logger.LogInformation("VacateNoticeJob: Terminated Lease {LeaseId} (notice {NoticeId})", lease.LeaseId, notice.VacateNoticeId);

            await _notif.SendAsync(new Notification
            {
                NotificationId = Guid.NewGuid(),
                UserId = lease.TenantId,
                Type = NotificationType.LeaseTerminated,
                Title = "Your Lease Has Been Terminated",
                Message = $"Your lease for '{property.Title}' has ended as of {notice.RequestedExitDate:dd MMM yyyy}.",
                EntityId = lease.LeaseId.ToString(),
                EntityType = "Lease",
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            });

            await _notif.SendAsync(new Notification
            {
                NotificationId = Guid.NewGuid(),
                UserId = property.OwnerId,
                Type = NotificationType.LeaseTerminated,
                Title = "Vacate Notice Processed — Property Now Active",
                Message = $"The tenant's vacate notice for '{property.Title}' has been processed. The property is now available for new tenants.",
                EntityId = property.PropertyId.ToString(),
                EntityType = "Property",
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            });
        }

        if (hasChanges)
            await _uow.SaveChangesAsync();
    }
}
