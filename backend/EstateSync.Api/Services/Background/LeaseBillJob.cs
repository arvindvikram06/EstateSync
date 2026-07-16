using EstateSync.Api.Models;
using EstateSync.Api.Repositories.Interfaces;
using Microsoft.Extensions.Logging;

namespace EstateSync.Api.Services.Background;

public class LeaseBillJob
{
    private readonly ILeaseRepository _leaseRepo;
    private readonly ILeaseBillRepository _leaseBillRepo;
    private readonly IUnitOfWork _uow;
    private readonly ILogger<LeaseBillJob> _logger;

    public LeaseBillJob(ILeaseRepository leaseRepo, ILeaseBillRepository leaseBillRepo, IUnitOfWork uow, ILogger<LeaseBillJob> logger)
    {
        _leaseRepo = leaseRepo; _leaseBillRepo = leaseBillRepo; _uow = uow; _logger = logger;
    }

    public async Task RunAsync()
    {
        var leases = await _leaseRepo.GetActiveLeasesWithBillsAsync();
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

            while (currentDueDate <= targetDate && currentDueDate <= lease.EndDate.Date)
            {
                int monthsElapsed = ((currentDueDate.Year - lease.StartDate.Year) * 12) + currentDueDate.Month - lease.StartDate.Month;

                if (monthsElapsed % multiplier == 0)
                {
                    bool billExists = lease.LeaseBills.Any(b => b.DueDate.Date == currentDueDate && b.BillType == LeaseBillType.Rent);

                    if (!billExists)
                    {
                        await _leaseBillRepo.AddAsync(new LeaseBill
                        {
                            LeaseBillId = Guid.NewGuid(),
                            LeaseId = lease.LeaseId,
                            BillType = LeaseBillType.Rent,
                            DueDate = currentDueDate,
                            Amount = lease.MonthlyRent * multiplier,
                            PendingAmount = lease.MonthlyRent * multiplier,
                            CreatedAt = DateTime.UtcNow
                        });

                        hasChanges = true;
                        _logger.LogInformation("LeaseBillJob: Generated bill for Lease {LeaseId} ({Frequency}) DueDate {DueDate}",
                            lease.LeaseId, lease.PaymentFrequency, currentDueDate);
                    }
                }

                currentDueDate = currentDueDate.AddMonths(1);
            }
        }

        if (hasChanges)
            await _uow.SaveChangesAsync();
    }
}
