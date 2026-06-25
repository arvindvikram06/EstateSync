using System;
using System.Threading.Tasks;
using EstateSync.Api.Models;

namespace EstateSync.Api.Repositories.Interfaces;

public interface IStripeConnectedAccountRepository : IRepository<StripeConnectedAccount>
{
    Task<StripeConnectedAccount?> GetByOwnerIdAsync(Guid ownerId);
    Task<StripeConnectedAccount?> GetByStripeAccountIdAsync(string stripeAccountId);
}
