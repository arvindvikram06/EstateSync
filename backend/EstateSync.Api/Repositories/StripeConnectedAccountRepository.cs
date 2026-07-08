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

public class StripeConnectedAccountRepository : BaseRepository<StripeConnectedAccount>, IStripeConnectedAccountRepository
{
    public StripeConnectedAccountRepository(AppDbContext context) : base(context) { }

    public async Task<StripeConnectedAccount?> GetByOwnerIdAsync(Guid ownerId)
        => await _dbSet.FirstOrDefaultAsync(x => x.OwnerId == ownerId);

    public async Task<StripeConnectedAccount?> GetByStripeAccountIdAsync(string stripeAccountId)
        => await _dbSet.FirstOrDefaultAsync(x => x.StripeAccountId == stripeAccountId);

    public async Task<StripeConnectedAccount?> GetByIdWithOwnerAsync(Guid id, CancellationToken ct = default)
        => await _dbSet.Include(a => a.Owner).FirstOrDefaultAsync(a => a.Id == id, ct);

    public async Task<(IEnumerable<StripeConnectedAccount> Items, int Total)> GetPagedWithOwnerAsync(
        int page, int limit, CancellationToken ct = default)
    {
        var query = _dbSet
            .AsNoTracking()
            .Include(a => a.Owner)
            .OrderByDescending(a => a.CreatedAt);

        var total = await query.CountAsync(ct);
        var items = await query
            .Skip((page - 1) * limit)
            .Take(limit)
            .ToListAsync(ct);

        return (items, total);
    }
}
