using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EstateSync.Api.DTOs.Admin;

namespace EstateSync.Api.Repositories.Interfaces;

public interface IStatisticsRepository
{
    Task<int> GetTotalUsersAsync(CancellationToken ct = default);
    /// <summary>All role counts in one grouped query (keyed by role name).</summary>
    Task<Dictionary<string, int>> GetUserCountsByRoleAsync(CancellationToken ct = default);
    Task<(decimal TotalRevenue, decimal TotalPlatformFees)> GetRevenueThisMonthAsync(CancellationToken ct = default);
    Task<Dictionary<string, int>> GetPropertyStatusBreakdownAsync(CancellationToken ct = default);
    Task<Dictionary<string, int>> GetLeaseStatusBreakdownAsync(CancellationToken ct = default);
    Task<Dictionary<string, int>> GetComplaintStatusBreakdownAsync(CancellationToken ct = default);
    Task<Dictionary<string, int>> GetKycStatusBreakdownAsync(CancellationToken ct = default);
    Task<List<TimeSeriesPoint>> GetNewUsersTimeSeriesAsync(int days, CancellationToken ct = default);
    Task<List<TimeSeriesPoint>> GetPaymentsTimeSeriesAsync(int days, CancellationToken ct = default);
    Task<Dictionary<string, int>>     GetBillStatusBreakdownAsync(CancellationToken ct = default);
    Task<Dictionary<string, decimal>> GetRevenueByBillTypeAsync(CancellationToken ct = default);
    Task<Dictionary<string, int>>     GetPayoutStatusBreakdownAsync(CancellationToken ct = default);
}
