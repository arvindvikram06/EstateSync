using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EstateSync.Api.Models;

namespace EstateSync.Api.Repositories.Interfaces;

public interface IOwnerSettlementRepository : IRepository<OwnerSettlement>
{
    Task<(IEnumerable<OwnerSettlement> Items, int Total)> GetPagedAsync(
        Guid? ownerId, string? status, DateTime? from, DateTime? to,
        int page, int limit, CancellationToken ct = default);

    Task<(decimal TotalPaid, decimal TotalPending, decimal TotalFailed)> GetTotalsAsync(
        CancellationToken ct = default);

    Task<(decimal TotalPaid, decimal TotalPending, int TotalPayouts, DateTime? LastPayoutDate, decimal? LastPayoutAmount)> GetOwnerTotalsAsync(
        Guid ownerId, CancellationToken ct = default);

    Task<OwnerSettlement?> GetByStripePayoutIdAsync(string stripePayoutId, CancellationToken ct = default);
}
