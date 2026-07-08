using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EstateSync.Api.Common;
using EstateSync.Api.DTOs.Admin;
using EstateSync.Api.Exceptions;
using EstateSync.Api.Interfaces.Services.Admin;
using EstateSync.Api.Repositories.Interfaces;
using Stripe;

namespace EstateSync.Api.Services.Admin;

public class AdminFinancialService : IAdminFinancialService
{
    private readonly IOwnerSettlementRepository _settlementRepo;
    private readonly IStripeConnectedAccountRepository _connectedAccountRepo;
    private readonly IPaymentRepository _paymentRepo;
    private readonly IUnitOfWork _unitOfWork;

    public AdminFinancialService(
        IOwnerSettlementRepository settlementRepo,
        IStripeConnectedAccountRepository connectedAccountRepo,
        IPaymentRepository paymentRepo,
        IUnitOfWork unitOfWork)
    {
        _settlementRepo      = settlementRepo      ?? throw new ArgumentNullException(nameof(settlementRepo));
        _connectedAccountRepo = connectedAccountRepo ?? throw new ArgumentNullException(nameof(connectedAccountRepo));
        _paymentRepo         = paymentRepo         ?? throw new ArgumentNullException(nameof(paymentRepo));
        _unitOfWork          = unitOfWork          ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<PagedResponse<OwnerSettlementResponse>> GetSettlementsAsync(
        Guid? ownerId, string? status, DateTime? from, DateTime? to,
        int page, int limit, CancellationToken ct = default)
    {
        var (items, total) = await _settlementRepo.GetPagedAsync(ownerId, status, from, to, page, limit, ct);
        var data = items.Select(s => new OwnerSettlementResponse(
            s.OwnerSettlementId, s.OwnerId,
            s.Owner?.FullName ?? string.Empty, s.Owner?.Email ?? string.Empty,
            s.StripePayoutId, s.Amount, s.Currency, s.DestinationId,
            s.ArrivalDate, s.Status, s.CreatedAt)).ToList();

        return new PagedResponse<OwnerSettlementResponse>
        {
            Data = data, Total = total, Page = page, Limit = limit
        };
    }

    public async Task<SettlementSummaryResponse> GetFinancialSummaryAsync(CancellationToken ct = default)
    {
        var (paid, pending, failed)   = await _settlementRepo.GetTotalsAsync(ct);
        var (volume, platformFees)    = await _paymentRepo.GetAllTimeTotalsAsync(ct);
        var totalPayouts              = await _settlementRepo.CountAsync(ct);

        return new SettlementSummaryResponse(
            paid, pending, failed, totalPayouts, platformFees, volume);
    }

    public async Task<PagedResponse<ConnectedAccountResponse>> GetConnectedAccountsAsync(
        int page, int limit, CancellationToken ct = default)
    {
        var (items, total) = await _connectedAccountRepo.GetPagedWithOwnerAsync(page, limit, ct);
        var data = items.Select(ToConnectedAccountResponse).ToList();

        return new PagedResponse<ConnectedAccountResponse>
        {
            Data = data, Total = total, Page = page, Limit = limit
        };
    }

    public async Task<ConnectedAccountResponse> SyncConnectedAccountAsync(Guid accountDbId, CancellationToken ct = default)
    {
        var record = await _connectedAccountRepo.GetByIdWithOwnerAsync(accountDbId, ct)
            ?? throw new NotFoundException("StripeConnectedAccount", accountDbId);

        var stripeService = new AccountService();
        var stripeAccount = await stripeService.GetAsync(record.StripeAccountId, cancellationToken: ct);

        record.ChargesEnabled   = stripeAccount.ChargesEnabled;
        record.PayoutsEnabled   = stripeAccount.PayoutsEnabled;
        record.DetailsSubmitted = stripeAccount.DetailsSubmitted;
        record.LastSyncedAt     = DateTime.UtcNow;

        // Entity is already tracked by EF from GetByIdAsync — UpdateAsync is a no-op for tracked entities
        await _connectedAccountRepo.UpdateAsync(record);
        await _unitOfWork.SaveChangesAsync(ct);

        return ToConnectedAccountResponse(record);
    }

    private static ConnectedAccountResponse ToConnectedAccountResponse(Models.StripeConnectedAccount a) =>
        new(a.Id, a.OwnerId,
            a.Owner?.FullName ?? string.Empty, a.Owner?.Email ?? string.Empty,
            a.StripeAccountId, a.ChargesEnabled, a.PayoutsEnabled,
            a.DetailsSubmitted, a.LastSyncedAt, a.CreatedAt);

    public async Task<PagedResponse<AdminPaymentResponse>> GetPaymentsAsync(
        string? status, DateTime? from, DateTime? to,
        int page, int limit, CancellationToken ct = default)
    {
        var (items, total) = await _paymentRepo.GetPagedWithDetailsAsync(status, from, to, page, limit, ct);
        var data = items.Select(p => new AdminPaymentResponse(
            p.PaymentId, p.LeaseBillId, p.PaidByUserId,
            p.PaidByUser?.FullName ?? string.Empty, p.PaidByUser?.Email ?? string.Empty,
            p.Amount, p.PlatformFee, p.TransactionId,
            p.PaymentStatus.ToString(), p.PaidDate, p.CreatedAt)).ToList();

        return new PagedResponse<AdminPaymentResponse>
        {
            Data = data, Total = total, Page = page, Limit = limit
        };
    }
}
