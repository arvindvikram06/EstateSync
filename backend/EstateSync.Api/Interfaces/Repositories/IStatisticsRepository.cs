using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EstateSync.Api.DTOs.Admin;

namespace EstateSync.Api.Repositories.Interfaces;

public interface IStatisticsRepository
{
    Task<int> GetTotalUsersAsync(CancellationToken ct = default);
    Task<int> GetUserCountByRoleAsync(string roleName, CancellationToken ct = default);
    Task<int> GetPendingKycCountAsync(CancellationToken ct = default);
    Task<int> GetPendingFinancialProfileCountAsync(CancellationToken ct = default);
    Task<int> GetPendingPropertyApprovalCountAsync(CancellationToken ct = default);
    Task<int> GetPendingLeaseApprovalCountAsync(CancellationToken ct = default);
    Task<int> GetOpenComplaintsCountAsync(CancellationToken ct = default);
    Task<int> GetActiveLeasesCountAsync(CancellationToken ct = default);
    Task<(decimal TotalRevenue, decimal TotalPlatformFees)> GetRevenueThisMonthAsync(CancellationToken ct = default);
    Task<Dictionary<string, int>> GetPropertyStatusBreakdownAsync(CancellationToken ct = default);
    Task<Dictionary<string, int>> GetLeaseStatusBreakdownAsync(CancellationToken ct = default);
    Task<Dictionary<string, int>> GetComplaintStatusBreakdownAsync(CancellationToken ct = default);
    Task<Dictionary<string, int>> GetKycStatusBreakdownAsync(CancellationToken ct = default);
    Task<List<TimeSeriesPoint>> GetNewUsersTimeSeriesAsync(int days, CancellationToken ct = default);
    Task<List<TimeSeriesPoint>> GetPaymentsTimeSeriesAsync(int days, CancellationToken ct = default);
}
