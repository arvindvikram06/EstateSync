using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using EstateSync.Api.Data;
using EstateSync.Api.Models;
using EstateSync.Api.Repositories.Interfaces;

namespace EstateSync.Api.Repositories;

public class StripeConnectedAccountRepository : BaseRepository<StripeConnectedAccount>, IStripeConnectedAccountRepository
{
    public StripeConnectedAccountRepository(AppDbContext context) : base(context)
    {
    }

    public async Task<StripeConnectedAccount?> GetByOwnerIdAsync(Guid ownerId)
    {
        return await _dbSet
            .FirstOrDefaultAsync(x => x.OwnerId == ownerId);
    }

    public async Task<StripeConnectedAccount?> GetByStripeAccountIdAsync(string stripeAccountId)
    {
        return await _dbSet
            .FirstOrDefaultAsync(x => x.StripeAccountId == stripeAccountId);
    }
}
