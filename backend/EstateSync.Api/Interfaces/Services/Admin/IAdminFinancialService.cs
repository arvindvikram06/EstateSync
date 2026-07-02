using System;
using System.Threading;
using System.Threading.Tasks;
using EstateSync.Api.Common;
using EstateSync.Api.DTOs.Admin;

namespace EstateSync.Api.Interfaces.Services.Admin;

public interface IAdminFinancialService
{
    Task<PagedResponse<OwnerSettlementResponse>> GetSettlementsAsync(Guid? ownerId, string? status, DateTime? from, DateTime? to, int page, int limit, CancellationToken ct = default);
    Task<SettlementSummaryResponse> GetFinancialSummaryAsync(CancellationToken ct = default);
    Task<PagedResponse<ConnectedAccountResponse>> GetConnectedAccountsAsync(int page, int limit, CancellationToken ct = default);
    Task<ConnectedAccountResponse> SyncConnectedAccountAsync(Guid accountDbId, CancellationToken ct = default);
    Task<PagedResponse<AdminPaymentResponse>> GetPaymentsAsync(string? status, DateTime? from, DateTime? to, int page, int limit, CancellationToken ct = default);
}
