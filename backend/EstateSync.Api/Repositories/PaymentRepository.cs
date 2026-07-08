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

public class PaymentRepository : BaseRepository<Payment>, IPaymentRepository
{
    public PaymentRepository(AppDbContext context) : base(context) { }

    public async Task<(IEnumerable<Payment> Items, int Total)> GetPagedWithDetailsAsync(
        string? status, DateTime? from, DateTime? to,
        int page, int limit, CancellationToken ct = default)
    {
        var query = _dbSet
            .AsNoTracking()
            .Include(p => p.PaidByUser)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<PaymentStatus>(status, true, out var parsedStatus))
            query = query.Where(p => p.PaymentStatus == parsedStatus);
        if (from.HasValue)
            query = query.Where(p => p.PaidDate >= from.Value);
        if (to.HasValue)
            query = query.Where(p => p.PaidDate <= to.Value);

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(p => p.PaidDate)
            .Skip((page - 1) * limit)
            .Take(limit)
            .ToListAsync(ct);

        return (items, total);
    }

    public async Task<(decimal TotalVolume, decimal TotalPlatformFees)> GetAllTimeTotalsAsync(CancellationToken ct = default)
    {
        var result = await _dbSet
            .Where(p => p.PaymentStatus == PaymentStatus.Paid)
            .GroupBy(_ => 1)
            .Select(g => new { Volume = g.Sum(p => p.Amount), Fees = g.Sum(p => p.PlatformFee) })
            .FirstOrDefaultAsync(ct);

        return result == null ? (0, 0) : (result.Volume, result.Fees);
    }

    public async Task<Payment?> GetByTransactionIdAsync(string transactionId, CancellationToken ct = default)
        => await _dbSet.FirstOrDefaultAsync(p => p.TransactionId == transactionId, ct);

    public async Task<IEnumerable<Payment>> GetByLeaseIdAsync(Guid leaseId, CancellationToken ct = default)
    {
        return await _dbSet
            .AsNoTracking()
            .Include(p => p.LeaseBill)
            .Where(p => p.LeaseBill != null && p.LeaseBill.LeaseId == leaseId)
            .OrderByDescending(p => p.PaidDate)
            .ToListAsync(ct);
    }
}
