using System.Collections.Generic;

namespace EstateSync.Api.DTOs.Admin;

public record TimeSeriesPoint(string Date, int Count, decimal Amount = 0);

public record AdminDashboardStatsResponse
{
    // KPI counts
    public int TotalUsers { get; init; }
    public int TotalOwners { get; init; }
    public int TotalTenants { get; init; }
    public int TotalVerifiers { get; init; }

    // Pending action counts (for shortcuts)
    public int PendingKycCount { get; init; }
    public int PendingFinancialProfileCount { get; init; }
    public int PendingPropertyApprovalCount { get; init; }
    public int PendingLeaseApprovalCount { get; init; }
    public int OpenComplaintsCount { get; init; }
    public int ActiveLeasesCount { get; init; }

    // Revenue (this calendar month)
    public decimal TotalRevenueThisMonth { get; init; }
    public decimal TotalPlatformFeesThisMonth { get; init; }

    // Status breakdowns (for donut charts)
    public Dictionary<string, int> PropertyStatusBreakdown { get; init; } = new();
    public Dictionary<string, int> LeaseStatusBreakdown { get; init; } = new();
    public Dictionary<string, int> ComplaintStatusBreakdown { get; init; } = new();
    public Dictionary<string, int> KycStatusBreakdown { get; init; } = new();

    // Time series — last 30 days (for area/line charts)
    public List<TimeSeriesPoint> NewUsersTimeSeries { get; init; } = new();
    public List<TimeSeriesPoint> PaymentsTimeSeries { get; init; } = new();
}
