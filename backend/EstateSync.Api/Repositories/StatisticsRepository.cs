using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using EstateSync.Api.Data;
using EstateSync.Api.DTOs.Admin;
using EstateSync.Api.Repositories.Interfaces;
using EstateSync.Api.Models;

namespace EstateSync.Api.Repositories;

public class StatisticsRepository : IStatisticsRepository
{
    private readonly AppDbContext _db;

    public StatisticsRepository(AppDbContext db) => _db = db;

    public Task<int> GetTotalUsersAsync(CancellationToken ct = default)
        => _db.Users.CountAsync(ct);

    public async Task<Dictionary<string, int>> GetUserCountsByRoleAsync(CancellationToken ct = default)
        => await _db.UserRoles
            .Where(ur => ur.Role != null && ur.User!.DeletedAt == null)
            .GroupBy(ur => ur.Role!.Name)
            .Select(g => new { Role = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Role, x => x.Count, ct);

    public async Task<(decimal TotalRevenue, decimal TotalPlatformFees)> GetRevenueThisMonthAsync(CancellationToken ct = default)
    {
        var start = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);
        var result = await _db.Payments
            .Where(p => p.PaymentStatus == PaymentStatus.Paid && p.PaidDate >= start)
            .GroupBy(_ => 1)
            .Select(g => new { Revenue = g.Sum(p => p.Amount), Fees = g.Sum(p => p.PlatformFee) })
            .FirstOrDefaultAsync(ct);
        return result == null ? (0, 0) : (result.Revenue, result.Fees);
    }

    public async Task<Dictionary<string, int>> GetPropertyStatusBreakdownAsync(CancellationToken ct = default)
    {
        return await _db.Properties
            .GroupBy(p => p.Status)
            .Select(g => new { Status = g.Key.ToString(), Count = g.Count() })
            .ToDictionaryAsync(x => x.Status, x => x.Count, ct);
    }

    public async Task<Dictionary<string, int>> GetLeaseStatusBreakdownAsync(CancellationToken ct = default)
    {
        return await _db.Leases
            .GroupBy(l => l.Status)
            .Select(g => new { Status = g.Key.ToString(), Count = g.Count() })
            .ToDictionaryAsync(x => x.Status, x => x.Count, ct);
    }

    public async Task<Dictionary<string, int>> GetComplaintStatusBreakdownAsync(CancellationToken ct = default)
    {
        return await _db.Complaints
            .GroupBy(c => c.Status)
            .Select(g => new { Status = g.Key.ToString(), Count = g.Count() })
            .ToDictionaryAsync(x => x.Status, x => x.Count, ct);
    }

    public async Task<Dictionary<string, int>> GetKycStatusBreakdownAsync(CancellationToken ct = default)
    {
        return await _db.KycVerifications
            .GroupBy(k => k.Status)
            .Select(g => new { Status = g.Key.ToString(), Count = g.Count() })
            .ToDictionaryAsync(x => x.Status, x => x.Count, ct);
    }

    public async Task<List<TimeSeriesPoint>> GetNewUsersTimeSeriesAsync(int days, CancellationToken ct = default)
    {
        var from = DateTime.UtcNow.Date.AddDays(-days + 1);
        var raw = await _db.Users
            .Where(u => u.CreatedAt >= from)
            .GroupBy(u => u.CreatedAt.Date)
            .Select(g => new { Date = g.Key, Count = g.Count() })
            .ToListAsync(ct);

        return Enumerable.Range(0, days)
            .Select(i => from.AddDays(i))
            .Select(d => new TimeSeriesPoint(
                d.ToString("yyyy-MM-dd"),
                raw.FirstOrDefault(r => r.Date == d)?.Count ?? 0))
            .ToList();
    }

    public async Task<List<TimeSeriesPoint>> GetPaymentsTimeSeriesAsync(int days, CancellationToken ct = default)
    {
        var from = DateTime.UtcNow.Date.AddDays(-days + 1);
        var raw = await _db.Payments
            .Where(p => p.PaymentStatus == PaymentStatus.Paid && p.PaidDate >= from)
            .GroupBy(p => p.PaidDate.Date)
            .Select(g => new { Date = g.Key, Count = g.Count(), Amount = g.Sum(p => p.Amount) })
            .ToListAsync(ct);

        return Enumerable.Range(0, days)
            .Select(i => from.AddDays(i))
            .Select(d =>
            {
                var match = raw.FirstOrDefault(r => r.Date == d);
                return new TimeSeriesPoint(d.ToString("yyyy-MM-dd"), match?.Count ?? 0, match?.Amount ?? 0);
            })
            .ToList();
    }

    public async Task<Dictionary<string, int>> GetBillStatusBreakdownAsync(CancellationToken ct = default)
    {
        var bills = await _db.LeaseBills
            .Select(b => new
            {
                HasPaid   = b.Payments.Any(p => p.PaymentStatus == PaymentStatus.Paid),
                HasFailed = b.Payments.Any(p => p.PaymentStatus == PaymentStatus.Failed),
            })
            .ToListAsync(ct);

        return new Dictionary<string, int>
        {
            ["Paid"]    = bills.Count(b => b.HasPaid),
            ["Failed"]  = bills.Count(b => !b.HasPaid && b.HasFailed),
            ["Pending"] = bills.Count(b => !b.HasPaid && !b.HasFailed),
        };
    }

    public async Task<Dictionary<string, decimal>> GetRevenueByBillTypeAsync(CancellationToken ct = default)
    {
        return await _db.Payments
            .Where(p => p.PaymentStatus == PaymentStatus.Paid)
            .Join(_db.LeaseBills,
                p => p.LeaseBillId,
                b => b.LeaseBillId,
                (p, b) => new { p.Amount, b.BillType })
            .GroupBy(x => x.BillType)
            .Select(g => new { Key = g.Key.ToString(), Total = g.Sum(x => x.Amount) })
            .ToDictionaryAsync(x => x.Key, x => x.Total, ct);
    }

    public async Task<Dictionary<string, int>> GetPayoutStatusBreakdownAsync(CancellationToken ct = default)
    {
        var raw = await _db.OwnerSettlements
            .GroupBy(s => s.Status)
            .Select(g => new { Key = g.Key, Count = g.Count() })
            .ToListAsync(ct);

        return raw.ToDictionary(
            x => char.ToUpper(x.Key[0]) + x.Key[1..],
            x => x.Count);
    }
}
