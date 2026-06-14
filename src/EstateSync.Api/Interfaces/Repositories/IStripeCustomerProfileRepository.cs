using System;
using System.Threading.Tasks;
using EstateSync.Api.Models;

namespace EstateSync.Api.Repositories.Interfaces;

public interface IStripeCustomerProfileRepository : IRepository<StripeCustomerProfile>
{
    Task<StripeCustomerProfile?> GetByTenantIdAsync(Guid tenantId);
}
