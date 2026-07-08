using System;
using EstateSync.Api.Common;
using System.Collections.Generic;
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
        var (revenue, fees)    = await _statsRepo.GetRevenueThisMonthAsync(ct);
        var roleCounts         = await _statsRepo.GetUserCountsByRoleAsync(ct);
        var propertyBreakdown  = await _statsRepo.GetPropertyStatusBreakdownAsync(ct);
        var leaseBreakdown     = await _statsRepo.GetLeaseStatusBreakdownAsync(ct);
        var complaintBreakdown = await _statsRepo.GetComplaintStatusBreakdownAsync(ct);
        var kycBreakdown       = await _statsRepo.GetKycStatusBreakdownAsync(ct);

        // The pending/active counts are derived from the breakdowns above
        // instead of issuing a separate COUNT query for each.
        return new AdminDashboardStatsResponse
        {
            TotalUsers                   = await _statsRepo.GetTotalUsersAsync(ct),
            TotalOwners                  = roleCounts.GetValueOrDefault(Roles.Owner),
            TotalTenants                 = roleCounts.GetValueOrDefault(Roles.Tenant),
            TotalVerifiers               = roleCounts.GetValueOrDefault(Roles.Verifier),
            PendingKycCount              = kycBreakdown.GetValueOrDefault("Pending"),
            PendingFinancialProfileCount = await _statsRepo.GetPendingFinancialProfileCountAsync(ct),
            PendingPropertyApprovalCount = propertyBreakdown.GetValueOrDefault("PendingApproval"),
            PendingLeaseApprovalCount    = leaseBreakdown.GetValueOrDefault("PendingAdminApproval"),
            OpenComplaintsCount          = complaintBreakdown.GetValueOrDefault("Open"),
            ActiveLeasesCount            = leaseBreakdown.GetValueOrDefault("Active"),
            TotalRevenueThisMonth        = revenue,
            TotalPlatformFeesThisMonth   = fees,
            PropertyStatusBreakdown      = propertyBreakdown,
            LeaseStatusBreakdown         = leaseBreakdown,
            ComplaintStatusBreakdown     = complaintBreakdown,
            KycStatusBreakdown           = kycBreakdown,
            BillStatusBreakdown          = await _statsRepo.GetBillStatusBreakdownAsync(ct),
            RevenueByBillType            = await _statsRepo.GetRevenueByBillTypeAsync(ct),
            PayoutStatusBreakdown        = await _statsRepo.GetPayoutStatusBreakdownAsync(ct),
            NewUsersTimeSeries           = await _statsRepo.GetNewUsersTimeSeriesAsync(30, ct),
            PaymentsTimeSeries           = await _statsRepo.GetPaymentsTimeSeriesAsync(30, ct),
        };
    }
}
