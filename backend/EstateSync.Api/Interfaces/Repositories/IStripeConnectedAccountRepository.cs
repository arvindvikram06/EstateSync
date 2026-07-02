using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EstateSync.Api.Models;

namespace EstateSync.Api.Repositories.Interfaces;

public interface IStripeConnectedAccountRepository : IRepository<StripeConnectedAccount>
{
    Task<StripeConnectedAccount?> GetByOwnerIdAsync(Guid ownerId);
    Task<StripeConnectedAccount?> GetByStripeAccountIdAsync(string stripeAccountId);
    Task<(IEnumerable<StripeConnectedAccount> Items, int Total)> GetPagedWithOwnerAsync(
        int page, int limit, CancellationToken ct = default);
}
