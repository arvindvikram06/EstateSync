using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using EstateSync.Api.Models;

namespace EstateSync.Api.Repositories.Interfaces;

public interface IComplaintRepository : IRepository<Complaint>
{
    Task<IEnumerable<Complaint>> GetByLeaseIdAsync(Guid leaseId);
    Task<IEnumerable<Complaint>> GetOpenComplaintsOlderThanDaysAsync(int days);
}
