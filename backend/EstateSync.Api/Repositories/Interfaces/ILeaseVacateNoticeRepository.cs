using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using EstateSync.Api.Models;

namespace EstateSync.Api.Repositories.Interfaces;

public interface ILeaseVacateNoticeRepository : IRepository<LeaseVacateNotice>
{
    Task<LeaseVacateNotice?> GetPendingByLeaseIdAsync(Guid leaseId);
    Task<LeaseVacateNotice?> GetByIdWithDetailsAsync(Guid vacateNoticeId);
    Task<IEnumerable<LeaseVacateNotice>> GetDueForTerminationAsync(DateTime asOf);
}
