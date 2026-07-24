using System;
using System.Threading;
using System.Threading.Tasks;

namespace EstateSync.Api.Interfaces.Services.Payments;

public interface IPlatformFeeService
{
    Task<decimal> GetPercentAsync(CancellationToken ct = default);
    Task<decimal> UpdatePercentAsync(decimal percentage, Guid adminUserId, CancellationToken ct = default);
}
