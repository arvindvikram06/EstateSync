using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using EstateSync.Api.Models;

namespace EstateSync.Api.Repositories.Interfaces;

public interface IKycVerificationRepository : IRepository<KycVerification>
{
    Task<KycVerification?> GetWithDetailsByIdAsync(Guid id);
    Task<IEnumerable<KycVerification>> GetByUserIdAsync(Guid userId);
    Task<(IEnumerable<KycVerification> Items, int Total)> GetPagedAsync(string? status, int page, int limit);
}
