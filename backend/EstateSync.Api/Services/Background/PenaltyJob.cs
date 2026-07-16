using EstateSync.Api.Interfaces.Services.Notifications;
using EstateSync.Api.Models;
using EstateSync.Api.Repositories.Interfaces;
using Microsoft.Extensions.Logging;

namespace EstateSync.Api.Services.Background;

public class PenaltyJob
{
    private readonly ILeaseBillRepository _leaseBillRepo;
    private readonly INotificationSender _notif;
    private readonly IUnitOfWork _uow;
    private readonly ILogger<PenaltyJob> _logger;

    public PenaltyJob(ILeaseBillRepository leaseBillRepo, INotificationSender notif, IUnitOfWork uow, ILogger<PenaltyJob> logger)
    {
        _leaseBillRepo = leaseBillRepo; _notif = notif; _uow = uow; _logger = logger;
    }

    public async Task RunAsync()
    {
        var overdueBills = await _leaseBillRepo.GetOverdueBillsWithLeaseAsync(DateTime.UtcNow);
        bool hasChanges = false;

        foreach (var bill in overdueBills)
        {
            var lease = bill.Lease;
            if (lease == null || lease.PenaltyPercentage == null || lease.PenaltyPercentage == 0)
                continue;

            bool penaltyExists = lease.LeaseBills != null &&
                lease.LeaseBills.Any(b => b.BillType == LeaseBillType.Penalty && b.DueDate.Date == bill.DueDate.Date);

            if (penaltyExists)
                continue;

            var penaltyAmount = Math.Round(bill.Amount * (lease.PenaltyPercentage.Value / 100m), 2);

            await _leaseBillRepo.AddAsync(new LeaseBill
            {
                LeaseBillId = Guid.NewGuid(),
                LeaseId = lease.LeaseId,
                BillType = LeaseBillType.Penalty,
                DueDate = bill.DueDate,
                Amount = penaltyAmount,
                PendingAmount = penaltyAmount,
                CreatedAt = DateTime.UtcNow
            });

            await _notif.SendAsync(new Notification
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
            _logger.LogInformation("PenaltyJob: Generated penalty of ₹{Amount} for Lease {LeaseId} (DueDate {DueDate})",
                penaltyAmount, lease.LeaseId, bill.DueDate);
        }

        if (hasChanges)
            await _uow.SaveChangesAsync();
    }
}
