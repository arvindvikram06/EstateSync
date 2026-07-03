using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EstateSync.Api.Common;
using EstateSync.Api.DTOs.Admin;
using EstateSync.Api.DTOs.Payments;
using EstateSync.Api.Exceptions;
using EstateSync.Api.Interfaces.Services.Payments;
using EstateSync.Api.Models;
using EstateSync.Api.Repositories.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Stripe;

namespace EstateSync.Api.Services.Payments;

public class StripeConnectedAccountService : IStripeConnectedAccountService
{
    private readonly IUserRepository _userRepo;
    private readonly IRoleRepository _roleRepo;
    private readonly IStripeConnectedAccountRepository _stripeAccountRepo;
    private readonly IOwnerSettlementRepository _settlementRepo;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IConfiguration _config;
    private readonly ILogger<StripeConnectedAccountService> _logger;

    public StripeConnectedAccountService(
        IUserRepository userRepo,
        IRoleRepository roleRepo,
        IStripeConnectedAccountRepository stripeAccountRepo,
        IOwnerSettlementRepository settlementRepo,
        IUnitOfWork unitOfWork,
        IConfiguration config,
        ILogger<StripeConnectedAccountService> logger)
    {
        _userRepo = userRepo;
        _roleRepo = roleRepo;
        _stripeAccountRepo = stripeAccountRepo;
        _settlementRepo = settlementRepo;
        _unitOfWork = unitOfWork;
        _config = config;
        _logger = logger;
    }

    public async Task<string> CreateOwnerOnboardingLinkAsync(Guid ownerId, CancellationToken ct = default)
    {
        var owner = await _userRepo.GetByIdAsync(ownerId);
        if (owner == null) throw new NotFoundException("User", ownerId);

        string stripeAccountId;
        var connectedAccount = await _stripeAccountRepo.GetByOwnerIdAsync(ownerId);

        if (connectedAccount == null)
        {
            var nameParts = owner.FullName.Split(' ', 2);
            var options = new AccountCreateOptions
            {
                Type    = "express",
                Country = "IN",
                Email = owner.Email,
                BusinessType = "individual",
                BusinessProfile = new AccountBusinessProfileOptions
                {
                    ProductDescription = "Real estate property owner receiving rent.",
                    Url = "https://estatesync.com"
                },
                Individual = new AccountIndividualOptions
                {
                    FirstName = nameParts.Length > 0 ? nameParts[0] : owner.FullName,
                    LastName = nameParts.Length > 1 ? nameParts[1] : "",
                    Email = owner.Email
                },
                Capabilities = new AccountCapabilitiesOptions
                {
                    Transfers = new AccountCapabilitiesTransfersOptions { Requested = true }
                },
                Settings = new AccountSettingsOptions
                {
                    Payouts = new AccountSettingsPayoutsOptions
                    {
                        Schedule = new AccountSettingsPayoutsScheduleOptions
                        {
                            Interval = "manual"
                        }
                    }
                }
            };
            var account = await new AccountService().CreateAsync(options, cancellationToken: ct);
            stripeAccountId = account.Id;

            await _stripeAccountRepo.AddAsync(new StripeConnectedAccount
            {
                OwnerId = owner.UserId,
                StripeAccountId = stripeAccountId
            });
            await _unitOfWork.SaveChangesAsync(ct);
        }
        else
        {
            stripeAccountId = connectedAccount.StripeAccountId;
            var account = await new AccountService().GetAsync(stripeAccountId, cancellationToken: ct);
            if (account.DetailsSubmitted && account.ChargesEnabled)
                throw new ConflictException("You have already completed your payment onboarding.");
        }

        var linkOptions = new AccountLinkCreateOptions
        {
            Account = stripeAccountId,
            RefreshUrl = _config["Stripe:OnboardingRefreshUrl"]
                ?? throw new InvalidOperationException("Stripe:OnboardingRefreshUrl is not configured."),
            ReturnUrl = _config["Stripe:OnboardingReturnUrl"]
                ?? throw new InvalidOperationException("Stripe:OnboardingReturnUrl is not configured."),
            Type = "account_onboarding",
        };
        var accountLink = await new AccountLinkService().CreateAsync(linkOptions, cancellationToken: ct);
        return accountLink.Url;
    }

    public async Task<string> CreateStripeLoginLinkAsync(Guid ownerId, CancellationToken ct = default)
    {
        var connectedAccount = await _stripeAccountRepo.GetByOwnerIdAsync(ownerId);
        if (connectedAccount == null)
            throw new NotFoundException("StripeConnectedAccount", ownerId);

        var loginLink = await new AccountLoginLinkService().CreateAsync(
            connectedAccount.StripeAccountId, new AccountLoginLinkCreateOptions(), cancellationToken: ct);
        return loginLink.Url;
    }

    public async Task<PagedResponse<OwnerSettlementResponse>> GetOwnerSettlementsAsync(
        Guid ownerId, string? status, int page, int limit, CancellationToken ct = default)
    {
        var (items, total) = await _settlementRepo.GetPagedAsync(ownerId, status, null, null, page, limit, ct);
        var data = items.Select(s => new OwnerSettlementResponse(
            s.OwnerSettlementId, s.OwnerId,
            s.Owner?.FullName ?? "", s.Owner?.Email ?? "",
            s.StripePayoutId, s.Amount, s.Currency,
            s.DestinationId, s.ArrivalDate, s.Status, s.CreatedAt));

        return new PagedResponse<OwnerSettlementResponse>
        {
            Data = data.ToList(),
            Page = page,
            Limit = limit,
            Total = total
        };
    }

    public async Task<OwnerEarningsSummaryResponse> GetOwnerEarningsSummaryAsync(Guid ownerId, CancellationToken ct = default)
    {
        var (paid, pending, total, lastDate, lastAmount) = await _settlementRepo.GetOwnerTotalsAsync(ownerId, ct);
        return new OwnerEarningsSummaryResponse(paid, pending, total, lastDate, lastAmount);
    }

    public async Task<OwnerAvailableBalanceResponse> GetAvailableBalanceAsync(Guid ownerId, CancellationToken ct = default)
    {
        var acct = await _stripeAccountRepo.GetByOwnerIdAsync(ownerId);
        if (acct == null) throw new NotFoundException("StripeConnectedAccount", ownerId);

        var balance = await new BalanceService().GetAsync(
            requestOptions: new RequestOptions { StripeAccount = acct.StripeAccountId },
            cancellationToken: ct);

        var availableEntry        = balance.Available.FirstOrDefault(b => b.Amount > 0) ?? balance.Available.FirstOrDefault();
        var instantAvailableEntry = balance.InstantAvailable?.FirstOrDefault(b => b.Amount > 0) ?? balance.InstantAvailable?.FirstOrDefault();
        var pendingEntry          = balance.Pending.FirstOrDefault(b => b.Amount > 0) ?? balance.Pending.FirstOrDefault();

        var currency = (availableEntry?.Currency ?? instantAvailableEntry?.Currency ?? pendingEntry?.Currency ?? "inr").ToUpperInvariant();

        return new OwnerAvailableBalanceResponse
        {
            Available        = (availableEntry?.Amount        ?? 0L) / 100m,
            InstantAvailable = (instantAvailableEntry?.Amount ?? 0L) / 100m,
            Pending          = (pendingEntry?.Amount          ?? 0L) / 100m,
            Currency         = currency,
            PayoutsEnabled   = acct.PayoutsEnabled,
        };
    }

    public async Task<RequestPayoutResponse> RequestPayoutAsync(Guid ownerId, string method, CancellationToken ct = default)
    {
        var isInstant = string.Equals(method, "instant", StringComparison.OrdinalIgnoreCase);

        var acct = await _stripeAccountRepo.GetByOwnerIdAsync(ownerId);
        if (acct == null) throw new NotFoundException("StripeConnectedAccount", ownerId);

        if (!acct.PayoutsEnabled)
            throw new BadRequestException("Payouts are not yet enabled on your Stripe account. Please complete onboarding.");

        var balance = await new BalanceService().GetAsync(
            requestOptions: new RequestOptions { StripeAccount = acct.StripeAccountId },
            cancellationToken: ct);

        var availableEntry        = balance.Available.FirstOrDefault(b => b.Amount > 0) ?? balance.Available.FirstOrDefault();
        var instantAvailableEntry = balance.InstantAvailable?.FirstOrDefault(b => b.Amount > 0) ?? balance.InstantAvailable?.FirstOrDefault();
        var payoutCurrency        = availableEntry?.Currency ?? instantAvailableEntry?.Currency ?? "inr";

        var availableInCents        = availableEntry?.Amount        ?? 0L;
        var instantAvailableInCents = instantAvailableEntry?.Amount ?? 0L;

        long amountInCents;
        if (isInstant)
        {
            if (instantAvailableInCents <= 0)
                throw new BadRequestException("No funds are available for instant payout.");
            amountInCents = instantAvailableInCents;
        }
        else
        {
            if (availableInCents <= 0)
            {
                if (instantAvailableInCents > 0)
                    throw new BadRequestException($"Your funds ({instantAvailableInCents / 100m:N0} {payoutCurrency.ToUpper()}) are still settling. Use Instant Payout to withdraw now, or wait 1–2 business days for Standard.");
                throw new BadRequestException("No funds are currently available to withdraw.");
            }
            amountInCents = availableInCents;
        }

        var payoutOptions = new PayoutCreateOptions
        {
            Amount   = amountInCents,
            Currency = payoutCurrency,
        };
        if (isInstant) payoutOptions.Method = "instant";

        var payout = await new PayoutService().CreateAsync(
            payoutOptions,
            new RequestOptions { StripeAccount = acct.StripeAccountId },
            cancellationToken: ct);

        _logger.LogInformation("{Method} payout {PayoutId} of {Amount} {Currency} initiated for Owner {OwnerId}",
            method, payout.Id, amountInCents / 100m, payoutCurrency.ToUpper(), ownerId);

        return new RequestPayoutResponse
        {
            StripePayoutId = payout.Id,
            Amount         = payout.Amount / 100m,
            Currency       = payout.Currency.ToUpperInvariant(),
            ArrivalDate    = payout.ArrivalDate,
            Status         = payout.Status,
            Method         = payout.Method ?? method,
        };
    }

    public async Task HandleAccountUpdatedAsync(string stripeAccountId, CancellationToken ct = default)
    {
        var connectedAccount = await _stripeAccountRepo.GetByStripeAccountIdAsync(stripeAccountId);
        if (connectedAccount == null) return;

        var account = await new AccountService().GetAsync(stripeAccountId, cancellationToken: ct);
        connectedAccount.ChargesEnabled = account.ChargesEnabled;
        connectedAccount.PayoutsEnabled = account.PayoutsEnabled;
        connectedAccount.DetailsSubmitted = account.DetailsSubmitted;
        connectedAccount.LastSyncedAt = DateTime.UtcNow;
        connectedAccount.UpdatedAt = DateTime.UtcNow;

        var owner = await _userRepo.GetByIdWithRolesAsync(connectedAccount.OwnerId);
        if (owner != null && account.ChargesEnabled && account.DetailsSubmitted && !owner.IsOwnerVerified)
        {
            owner.IsOwnerVerified = true;
            owner.UpdatedAt = DateTime.UtcNow;

            if (!owner.UserRoles.Any(ur => ur.Role?.Name == "Owner"))
            {
                var ownerRole = await _roleRepo.GetByNameAsync("Owner");
                if (ownerRole != null)
                    owner.UserRoles.Add(new UserRole { UserId = owner.UserId, RoleId = ownerRole.RoleId });
            }

            _logger.LogInformation("Owner {OwnerId} Stripe onboarding complete — Owner role assigned", connectedAccount.OwnerId);
        }

        await _unitOfWork.SaveChangesAsync(ct);
    }

    public async Task HandlePayoutPaidAsync(string stripePayoutJson, string connectedAccountId, CancellationToken ct = default)
    {
        var connectedAccount = await _stripeAccountRepo.GetByStripeAccountIdAsync(connectedAccountId);
        if (connectedAccount == null) return;

        var payout = Payout.FromJson(stripePayoutJson);

        // Idempotency guard: Stripe retries webhooks on non-2xx responses.
        // StripePayoutId is globally unique per payout — skip if already recorded.
        var existing = await _settlementRepo.GetByStripePayoutIdAsync(payout.Id, ct);
        if (existing != null)
        {
            _logger.LogWarning(
                "Duplicate webhook: OwnerSettlement for StripePayoutId {PayoutId} already exists (SettlementId={SettlementId}). Skipping.",
                payout.Id, existing.OwnerSettlementId);
            return;
        }

        await _settlementRepo.AddAsync(new OwnerSettlement
        {
            OwnerSettlementId = Guid.NewGuid(),
            OwnerId           = connectedAccount.OwnerId,
            StripePayoutId    = payout.Id,
            Amount            = payout.Amount / 100m,
            Currency          = payout.Currency,
            DestinationId     = payout.DestinationId ?? "N/A",
            ArrivalDate       = payout.ArrivalDate,
            Status            = payout.Status
        });
        await _unitOfWork.SaveChangesAsync(ct);
        _logger.LogInformation("Settlement recorded for Owner {OwnerId}, PayoutId {PayoutId}", connectedAccount.OwnerId, payout.Id);
    }
}
