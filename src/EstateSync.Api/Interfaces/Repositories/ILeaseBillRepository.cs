using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using EstateSync.Api.Models;

namespace EstateSync.Api.Repositories.Interfaces;

public interface ILeaseBillRepository : IRepository<LeaseBill>
{
    Task<IEnumerable<LeaseBill>> GetByLeaseIdAsync(Guid leaseId);
    Task<IEnumerable<LeaseBill>> GetOverdueBillsAsync(DateTime currentDate);
}
