using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using EstateSync.Api.Models;

namespace EstateSync.Api.Repositories.Interfaces;

public interface IKycVerificationRepository : IRepository<KycVerification>
{
    Task<KycVerification?> GetWithDetailsByIdAsync(Guid id);
    Task<IEnumerable<KycVerification>> GetByUserIdAsync(Guid userId);
    Task<(IEnumerable<KycVerification> Items, int Total)> GetPagedForListAsync(string? status, int page, int limit);
    Task<(IEnumerable<KycVerification> Items, int Total)> GetPagedAsync(string? status, int page, int limit);
    Task<bool> IsAadhaarApprovedAsync(string aadhaarNumber, Guid excludeUserId);
    Task<bool> IsPanApprovedAsync(string panNumber, Guid excludeUserId);
}
