using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EstateSync.Api.Models;

namespace EstateSync.Api.Repositories.Interfaces;

public interface IPaymentRepository : IRepository<Payment>
{
    Task<(IEnumerable<Payment> Items, int Total)> GetPagedWithDetailsAsync(
        string? status, DateTime? from, DateTime? to,
        int page, int limit, CancellationToken ct = default);

    Task<(decimal TotalVolume, decimal TotalPlatformFees)> GetAllTimeTotalsAsync(CancellationToken ct = default);

    Task<Payment?> GetByTransactionIdAsync(string transactionId, CancellationToken ct = default);

    Task<IEnumerable<Payment>> GetByLeaseIdAsync(Guid leaseId, CancellationToken ct = default);
}
