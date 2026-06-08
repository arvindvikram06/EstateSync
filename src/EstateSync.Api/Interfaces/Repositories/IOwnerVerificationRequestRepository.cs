using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using EstateSync.Api.Models;

namespace EstateSync.Api.Repositories.Interfaces;

public interface IOwnerVerificationRequestRepository : IRepository<OwnerVerificationRequest>
{
    Task<OwnerVerificationRequest?> GetWithDetailsByIdAsync(Guid id);
    Task<IEnumerable<OwnerVerificationRequest>> GetByUserIdAsync(Guid userId);
    Task<(IEnumerable<OwnerVerificationRequest> Items, int Total)> GetPagedRequestsAsync(string? status, int page, int limit);
}
