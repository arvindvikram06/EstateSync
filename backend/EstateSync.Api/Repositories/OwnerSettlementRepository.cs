using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using EstateSync.Api.Data;
using EstateSync.Api.Models;
using EstateSync.Api.Repositories.Interfaces;

namespace EstateSync.Api.Repositories;

public class OwnerSettlementRepository : BaseRepository<OwnerSettlement>, IOwnerSettlementRepository
{
    public OwnerSettlementRepository(AppDbContext context) : base(context) { }

    public async Task<(IEnumerable<OwnerSettlement> Items, int Total)> GetPagedAsync(
        Guid? ownerId, string? status, DateTime? from, DateTime? to,
        int page, int limit, CancellationToken ct = default)
    {
        var query = _dbSet
            .AsNoTracking()
            .Include(s => s.Owner)
            .AsQueryable();

        if (ownerId.HasValue)
            query = query.Where(s => s.OwnerId == ownerId.Value);
        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(s => s.Status == status);
        if (from.HasValue)
            query = query.Where(s => s.ArrivalDate >= from.Value);
        if (to.HasValue)
            query = query.Where(s => s.ArrivalDate <= to.Value);

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(s => s.ArrivalDate)
            .Skip((page - 1) * limit)
            .Take(limit)
            .ToListAsync(ct);

        return (items, total);
    }

    public async Task<(decimal TotalPaid, decimal TotalPending, decimal TotalFailed)> GetTotalsAsync(
        CancellationToken ct = default)
    {
        var totals = await _dbSet
            .GroupBy(s => s.Status)
            .Select(g => new { Status = g.Key, Total = g.Sum(s => s.Amount) })
            .ToListAsync(ct);

        var paid    = totals.FirstOrDefault(t => t.Status == "paid")?.Total ?? 0;
        var pending = totals.FirstOrDefault(t => t.Status == "pending")?.Total ?? 0;
        var failed  = totals.FirstOrDefault(t => t.Status == "failed")?.Total ?? 0;

        return (paid, pending, failed);
    }

    public async Task<(decimal TotalPaid, decimal TotalPending, int TotalPayouts, DateTime? LastPayoutDate, decimal? LastPayoutAmount)> GetOwnerTotalsAsync(
        Guid ownerId, CancellationToken ct = default)
    {
        var settlements = await _dbSet
            .AsNoTracking()
            .Where(s => s.OwnerId == ownerId)
            .OrderByDescending(s => s.ArrivalDate)
            .ToListAsync(ct);

        var paid    = settlements.Where(s => s.Status == "paid").Sum(s => s.Amount);
        var pending = settlements.Where(s => s.Status == "pending").Sum(s => s.Amount);
        var total   = settlements.Count;
        var last    = settlements.FirstOrDefault(s => s.Status == "paid");

        return (paid, pending, total, last?.ArrivalDate, last?.Amount);
    }

    public async Task<OwnerSettlement?> GetByStripePayoutIdAsync(string stripePayoutId, CancellationToken ct = default)
        => await _dbSet.FirstOrDefaultAsync(s => s.StripePayoutId == stripePayoutId, ct);
}
