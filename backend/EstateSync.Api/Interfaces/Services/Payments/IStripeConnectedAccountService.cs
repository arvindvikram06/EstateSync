using System;
using System.Threading;
using System.Threading.Tasks;
using EstateSync.Api.Common;
using EstateSync.Api.DTOs.Admin;
using EstateSync.Api.DTOs.Payments;

namespace EstateSync.Api.Interfaces.Services.Payments;

public interface IStripeConnectedAccountService
{
    Task<string> CreateOwnerOnboardingLinkAsync(Guid ownerId, CancellationToken ct = default);
    Task<string> CreateStripeLoginLinkAsync(Guid ownerId, CancellationToken ct = default);
    Task<PagedResponse<OwnerSettlementResponse>> GetOwnerSettlementsAsync(Guid ownerId, string? status, int page, int limit, CancellationToken ct = default);
    Task<OwnerEarningsSummaryResponse> GetOwnerEarningsSummaryAsync(Guid ownerId, CancellationToken ct = default);
    Task HandleAccountUpdatedAsync(string stripeAccountId, CancellationToken ct = default);
    Task HandlePayoutPaidAsync(string stripePayoutJson, string connectedAccountId, CancellationToken ct = default);
}
