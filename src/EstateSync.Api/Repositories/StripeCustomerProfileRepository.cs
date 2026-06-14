using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using EstateSync.Api.Data;
using EstateSync.Api.Models;
using EstateSync.Api.Repositories.Interfaces;

namespace EstateSync.Api.Repositories;

public class StripeCustomerProfileRepository : BaseRepository<StripeCustomerProfile>, IStripeCustomerProfileRepository
{
    public StripeCustomerProfileRepository(AppDbContext context) : base(context)
    {
    }

    public async Task<StripeCustomerProfile?> GetByTenantIdAsync(Guid tenantId)
    {
        return await _dbSet
            .FirstOrDefaultAsync(x => x.TenantId == tenantId);
    }
}
