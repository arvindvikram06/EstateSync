using System;
using System.Threading;
using System.Threading.Tasks;
using EstateSync.Api.DTOs.Admin;
using EstateSync.Api.Repositories.Interfaces;
using EstateSync.Api.Interfaces.Services.Admin;

namespace EstateSync.Api.Services.Admin;

public class AdminDashboardService : IAdminDashboardService
{
    private readonly IStatisticsRepository _statsRepo;

    public AdminDashboardService(IStatisticsRepository statsRepo)
    {
        _statsRepo = statsRepo ?? throw new ArgumentNullException(nameof(statsRepo));
    }

    public async Task<AdminDashboardStatsResponse> GetStatsAsync(CancellationToken ct = default)
    {
        var (revenue, fees) = await _statsRepo.GetRevenueThisMonthAsync(ct);

        return new AdminDashboardStatsResponse
        {
            TotalUsers                   = await _statsRepo.GetTotalUsersAsync(ct),
            TotalOwners                  = await _statsRepo.GetUserCountByRoleAsync("Owner", ct),
            TotalTenants                 = await _statsRepo.GetUserCountByRoleAsync("Tenant", ct),
            TotalVerifiers               = await _statsRepo.GetUserCountByRoleAsync("Verifier", ct),
            PendingKycCount              = await _statsRepo.GetPendingKycCountAsync(ct),
            PendingFinancialProfileCount = await _statsRepo.GetPendingFinancialProfileCountAsync(ct),
            PendingPropertyApprovalCount = await _statsRepo.GetPendingPropertyApprovalCountAsync(ct),
            PendingLeaseApprovalCount    = await _statsRepo.GetPendingLeaseApprovalCountAsync(ct),
            OpenComplaintsCount          = await _statsRepo.GetOpenComplaintsCountAsync(ct),
            ActiveLeasesCount            = await _statsRepo.GetActiveLeasesCountAsync(ct),
            TotalRevenueThisMonth        = revenue,
            TotalPlatformFeesThisMonth   = fees,
            PropertyStatusBreakdown      = await _statsRepo.GetPropertyStatusBreakdownAsync(ct),
            LeaseStatusBreakdown         = await _statsRepo.GetLeaseStatusBreakdownAsync(ct),
            ComplaintStatusBreakdown     = await _statsRepo.GetComplaintStatusBreakdownAsync(ct),
            KycStatusBreakdown           = await _statsRepo.GetKycStatusBreakdownAsync(ct),
            NewUsersTimeSeries           = await _statsRepo.GetNewUsersTimeSeriesAsync(30, ct),
            PaymentsTimeSeries           = await _statsRepo.GetPaymentsTimeSeriesAsync(30, ct),
        };
    }
}
