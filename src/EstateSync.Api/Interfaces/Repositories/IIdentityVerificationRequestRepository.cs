using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using EstateSync.Api.Models;

namespace EstateSync.Api.Repositories.Interfaces;

public interface IIdentityVerificationRequestRepository : IRepository<IdentityVerificationRequest>
{
    Task<IdentityVerificationRequest?> GetWithDetailsByIdAsync(Guid id);
    Task<IEnumerable<IdentityVerificationRequest>> GetByUserIdAsync(Guid userId);
    Task<(IEnumerable<IdentityVerificationRequest> Items, int Total)> GetPagedRequestsAsync(string? status, int page, int limit);
}
