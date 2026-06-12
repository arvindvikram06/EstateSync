using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using EstateSync.Api.Models;

namespace EstateSync.Api.Repositories.Interfaces;

public interface IComplaintRepository : IRepository<Complaint>
{
    Task<IEnumerable<Complaint>> GetByLeaseIdAsync(Guid leaseId);

    // Reserved for background escalation job \u2014 not yet implemented
    // Task<IEnumerable<Complaint>> GetOpenComplaintsOlderThanDaysAsync(int days);
}
