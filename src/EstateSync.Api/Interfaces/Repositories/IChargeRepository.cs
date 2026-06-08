using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using EstateSync.Api.Models;

namespace EstateSync.Api.Repositories.Interfaces;

public interface IChargeRepository : IRepository<Charge>
{
    Task<IEnumerable<Charge>> GetByLeaseIdAsync(Guid leaseId);
    Task<IEnumerable<Charge>> GetOverdueChargesAsync(DateTime currentDate);
}
