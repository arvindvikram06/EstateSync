using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EstateSync.Api.DTOs.Leases;
using EstateSync.Api.Exceptions;
using EstateSync.Api.Interfaces.Services.Notifications;
using EstateSync.Api.Interfaces.Services.Payments;
using EstateSync.Api.Models;
using EstateSync.Api.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Stripe;
using Stripe.Checkout;

namespace EstateSync.Api.Services.Payments;

public class TenantPaymentService : ITenantPaymentService
{
    private readonly IUserRepository _userRepo;
    private readonly ILeaseBillRepository _leaseBillRepo;
    private readonly IStripeConnectedAccountRepository _stripeAccountRepo;
    private readonly IStripeCustomerProfileRepository _stripeCustomerProfileRepo;
    private readonly IPaymentRepository _paymentRepo;
    private readonly INotificationSender _notificationSender;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IConfiguration _config;
    private readonly ILogger<TenantPaymentService> _logger;

    public TenantPaymentService(
        IUserRepository userRepo,
        ILeaseBillRepository leaseBillRepo,
        IStripeConnectedAccountRepository stripeAccountRepo,
        IStripeCustomerProfileRepository stripeCustomerProfileRepo,
        IPaymentRepository paymentRepo,
        INotificationSender notificationSender,
        IUnitOfWork unitOfWork,
        IConfiguration config,
        ILogger<TenantPaymentService> logger)
    {
        _userRepo = userRepo;
        _leaseBillRepo = leaseBillRepo;
        _stripeAccountRepo = stripeAccountRepo;
        _stripeCustomerProfileRepo = stripeCustomerProfileRepo;
        _paymentRepo = paymentRepo;
        _notificationSender = notificationSender;
        _unitOfWork = unitOfWork;
        _config = config;
        _logger = logger;
    }

    public async Task<string> CreateTenantCheckoutSessionAsync(Guid leaseBillId, Guid tenantId, CancellationToken ct = default)
    {
        var bill = await _leaseBillRepo.GetByIdWithLeaseAndPropertyAsync(leaseBillId);
        if (bill == null) throw new NotFoundException("LeaseBill", leaseBillId);

        // Fix #3: Enforce that only the tenant who owns this bill can pay it.
        if (bill.Lease?.TenantId != tenantId)
            throw new ForbiddenException("You are not authorized to pay this bill.");

        if (bill.PendingAmount <= 0)
            throw new ConflictException("This bill has already been fully paid.");

        // Fix #1: Reuse an existing open Stripe session instead of creating a second one.
        // This prevents two-tab races where both tabs see PendingAmount > 0 and both create
        // a session — which can result in the tenant being charged twice.
        if (bill.ActiveStripeSessionId != null)
        {
            try
            {
                var existingSession = await new SessionService().GetAsync(bill.ActiveStripeSessionId, cancellationToken: ct);
                if (existingSession.Status == "open")
                {
                    _logger.LogInformation(
                        "Reusing open Stripe session {SessionId} for Bill {BillId}.",
                        existingSession.Id, leaseBillId);
                    return existingSession.Url;
                }
                // Session is no longer open (expired/completed) — fall through to create a new one.
                bill.ActiveStripeSessionId = null;
            }
            catch (StripeException ex)
            {
                _logger.LogWarning(ex, "Could not retrieve existing session {SessionId}. Creating a new one.", bill.ActiveStripeSessionId);
                bill.ActiveStripeSessionId = null;
            }
        }

        var ownerAccount = await _stripeAccountRepo.GetByOwnerIdAsync(bill.Lease!.Property!.OwnerId);
        if (ownerAccount == null) throw new BadRequestException("Owner has not set up payments yet.");

        var tenant = await _userRepo.GetByIdAsync(tenantId);
        if (tenant == null) throw new NotFoundException("User", tenantId);

        var stripeCustomerId = await GetOrCreateStripeCustomerAsync(tenant, tenantId, ct);

        var (totalCents, appFeeCents, platformFee) = CalculateAmounts(bill.Amount);

        // Correlation metadata set on both Session and PaymentIntent so any payment-level
        // webhook can look up our DB records without an extra Stripe API round-trip.
        var correlationMetadata = new Dictionary<string, string>
        {
            { "LeaseBillId", bill.LeaseBillId.ToString() },
            { "TenantId",    tenantId.ToString() }
        };

        var baseSuccessUrl = _config["Stripe:CheckoutSuccessUrl"]
            ?? throw new InvalidOperationException("Stripe:CheckoutSuccessUrl is not configured.");

        var options = new SessionCreateOptions
        {
            Customer           = stripeCustomerId,
            PaymentMethodTypes = new List<string> { "card" },
            LineItems          = new List<SessionLineItemOptions>
            {
                new SessionLineItemOptions
                {
                    PriceData = new SessionLineItemPriceDataOptions
                    {
                        UnitAmount  = totalCents,
                        Currency    = "inr",
                        ProductData = new SessionLineItemPriceDataProductDataOptions
                        {
                            Name = $"Rent for {bill.Lease!.Property!.Title} - {bill.DueDate:MMM yyyy}"
                        },
                    },
                    Quantity = 1,
                },
            },
            Mode = "payment",
            PaymentIntentData = new SessionPaymentIntentDataOptions
            {
                ApplicationFeeAmount = appFeeCents,
                TransferData         = new SessionPaymentIntentDataTransferDataOptions
                {
                    Destination = ownerAccount.StripeAccountId,
                },
                Metadata = correlationMetadata,
            },
            Metadata   = correlationMetadata,
            // Append billId so the success page can poll for the specific bill flipping to Paid.
            SuccessUrl = $"{baseSuccessUrl}&billId={bill.LeaseBillId}",
            CancelUrl  = _config["Stripe:CheckoutCancelUrl"]
                ?? throw new InvalidOperationException("Stripe:CheckoutCancelUrl is not configured."),
        };

        var session = await new SessionService().CreateAsync(options, cancellationToken: ct);

        // Persist the session ID on the bill so concurrent requests reuse it instead of
        // creating a second chargeable session.
        bill.ActiveStripeSessionId = session.Id;
        await _unitOfWork.SaveChangesAsync(ct);

        return session.Url;
    }

    public async Task<IEnumerable<LeaseBillResponse>> GetTenantBillsAsync(Guid tenantId, CancellationToken ct = default)
    {
        var bills = await _leaseBillRepo.GetByTenantIdAsync(tenantId);
        return bills.Select(b => new LeaseBillResponse
        {
            LeaseBillId   = b.LeaseBillId,
            LeaseId       = b.LeaseId,
            PropertyTitle = b.Lease?.Property?.Title ?? "Unknown Property",
            BillType      = b.BillType,
            DueDate       = b.DueDate,
            Amount        = b.Amount,
            PendingAmount = b.PendingAmount
        });
    }

    public async Task HandleCheckoutCompletedAsync(string sessionJson, CancellationToken ct = default)
    {
        var session = Session.FromJson(sessionJson);

        if (session.PaymentStatus != "paid") return;

        if (!TryExtractMetadata(session.Metadata, session.Id, out var leaseBillId, out var tenantId))
            return;

        var transactionId = session.PaymentIntentId ?? session.Id;

        // Idempotency guard 1: skip if this exact transaction was already recorded.
        var existingPayment = await _paymentRepo.GetByTransactionIdAsync(transactionId, ct);
        if (existingPayment != null)
        {
            _logger.LogWarning(
                "Duplicate webhook: Payment for TransactionId {TxId} already exists (PaymentId={PaymentId}). Skipping.",
                transactionId, existingPayment.PaymentId);
            return;
        }

        var bill = await _leaseBillRepo.GetByIdWithLeaseAndPropertyAsync(leaseBillId);

        // Idempotency guard 2: skip if the bill is already fully settled.
        if (bill != null && bill.PendingAmount == 0m)
        {
            _logger.LogWarning(
                "Duplicate webhook: Bill {BillId} is already fully paid. Skipping TransactionId {TxId}.",
                leaseBillId, transactionId);
            return;
        }

        var (_, _, platformFee) = CalculateAmounts(bill?.Amount ?? (session.AmountTotal ?? 0) / 100m);
        var paidAmount = (session.AmountTotal ?? 0) / 100m;

        if (bill != null)
        {
            bill.PendingAmount        = 0m;
            bill.ActiveStripeSessionId = null; // session is complete — allow future bills to create new sessions
        }

        await _paymentRepo.AddAsync(new Payment
        {
            PaymentId       = Guid.NewGuid(),
            LeaseBillId     = leaseBillId,
            PaidByUserId    = tenantId,
            Amount          = paidAmount,
            PlatformFee     = platformFee,  // Fix #8: derived from CalculateAmounts, not hardcoded 2%
            TransactionId   = transactionId,
            GatewayResponse = session.ToJson(), // Fix #7: store real Stripe session data for audit/disputes
            PaymentStatus   = PaymentStatus.Paid,
            PaidDate        = DateTime.UtcNow
        });

        await _notificationSender.SendAsync(new Notification
        {
            NotificationId = Guid.NewGuid(),
            UserId         = tenantId,
            Type           = NotificationType.PaymentReceived,
            Title          = "Payment Successful",
            Message        = $"Your payment of ₹{paidAmount:N0} for bill due {bill?.DueDate:dd MMM yyyy} has been received.",
            EntityId       = leaseBillId.ToString(),
            EntityType     = "LeaseBill",
            IsRead         = false,
            CreatedAt      = DateTime.UtcNow
        });

        if (bill?.Lease?.Property != null)
        {
            await _notificationSender.SendAsync(new Notification
            {
                NotificationId = Guid.NewGuid(),
                UserId         = bill.Lease.Property.OwnerId,
                Type           = NotificationType.PaymentReceived,
                Title          = "Rent Payment Received",
                Message        = $"A rent payment of ₹{paidAmount:N0} has been received for '{bill.Lease.Property.Title}'.",
                EntityId       = leaseBillId.ToString(),
                EntityType     = "LeaseBill",
                IsRead         = false,
                CreatedAt      = DateTime.UtcNow
            });
        }

        await _unitOfWork.SaveChangesAsync(ct);
        _logger.LogInformation("Payment processed for Bill {BillId}, TransactionId {TxId}", leaseBillId, transactionId);
    }

    // Fires when a Stripe Checkout session expires after 24 hours without a completed payment.
    // The bill stays Pending. We clear ActiveStripeSessionId so the tenant can create a new session.
    public async Task HandleSessionExpiredAsync(string sessionJson, CancellationToken ct = default)
    {
        var session = Session.FromJson(sessionJson);

        if (!TryExtractMetadata(session.Metadata, session.Id, out var leaseBillId, out var tenantId))
            return;

        _logger.LogInformation("Checkout session {SessionId} expired without payment for Bill {BillId}.", session.Id, leaseBillId);

        var bill = await _leaseBillRepo.GetByIdWithLeaseAndPropertyAsync(leaseBillId);
        if (bill != null)
            bill.ActiveStripeSessionId = null;

        await _notificationSender.SendAsync(new Notification
        {
            NotificationId = Guid.NewGuid(),
            UserId         = tenantId,
            Type           = NotificationType.PaymentFailed,
            Title          = "Payment Not Completed",
            Message        = "Your payment session expired before completion. Please return to your bills to try again.",
            EntityId       = leaseBillId.ToString(),
            EntityType     = "LeaseBill",
            IsRead         = false,
            CreatedAt      = DateTime.UtcNow
        });

        await _unitOfWork.SaveChangesAsync(ct);
    }

    // ── Private helpers ──────────────────────────────────────────────────────

    private async Task<string> GetOrCreateStripeCustomerAsync(User tenant, Guid tenantId, CancellationToken ct)
    {
        var existing = await _stripeCustomerProfileRepo.GetByTenantIdAsync(tenantId);
        if (existing != null) return existing.StripeCustomerId;

        var customer = await new CustomerService().CreateAsync(
            new CustomerCreateOptions { Email = tenant.Email, Name = tenant.FullName },
            cancellationToken: ct);

        await _stripeCustomerProfileRepo.AddAsync(new StripeCustomerProfile
        {
            TenantId         = tenantId,
            StripeCustomerId = customer.Id
        });

        try
        {
            await _unitOfWork.SaveChangesAsync(ct);
        }
        catch (DbUpdateException)
        {
            // Fix #2: Another concurrent request won the race and saved first.
            // The Stripe customer we just created is orphaned — acceptable, Stripe cleans
            // up unused customers. Re-read the winner's profile and use that ID.
            _logger.LogWarning(
                "Concurrent StripeCustomerProfile insert for Tenant {TenantId}. Re-reading winner.", tenantId);
            var winner = await _stripeCustomerProfileRepo.GetByTenantIdAsync(tenantId);
            if (winner != null) return winner.StripeCustomerId;
            throw;
        }

        return customer.Id;
    }

    // Fix #9: Use InvariantCulture so "0.054" parses correctly on any server locale.
    private (long totalCents, long appFeeCents, decimal platformFee) CalculateAmounts(decimal billAmount)
    {
        double stripePercent   = double.TryParse(_config["Stripe:StripeFeePercent"],   NumberStyles.Float, CultureInfo.InvariantCulture, out var sp) ? sp : 0.054;
        double stripeFixedInr  = double.TryParse(_config["Stripe:StripeFeeFixedInr"],  NumberStyles.Float, CultureInfo.InvariantCulture, out var sf) ? sf : 25.0;
        double platformPercent = double.TryParse(_config["Stripe:PlatformFeePercent"], NumberStyles.Float, CultureInfo.InvariantCulture, out var pf) ? pf : 0.02;

        double rent         = (double)billAmount;
        double totalCharged = (rent + stripeFixedInr) / (1.0 - stripePercent);

        long totalCents    = (long)Math.Round(totalCharged * 100);
        long rentCents     = (long)Math.Round(rent * 100);
        long stripFeeCents = totalCents - rentCents;
        long platformCents = (long)Math.Round(rent * platformPercent * 100);
        long appFeeCents   = platformCents + stripFeeCents;

        return (totalCents, appFeeCents, platformCents / 100m);
    }

    private bool TryExtractMetadata(
        IDictionary<string, string> metadata, string sessionId,
        out Guid leaseBillId, out Guid tenantId)
    {
        leaseBillId = default;
        tenantId    = default;

        if (!metadata.TryGetValue("LeaseBillId", out var billIdStr) || !Guid.TryParse(billIdStr, out leaseBillId) ||
            !metadata.TryGetValue("TenantId",    out var tenantIdStr) || !Guid.TryParse(tenantIdStr, out tenantId))
        {
            _logger.LogWarning("Stripe session {SessionId} is missing required metadata (LeaseBillId/TenantId). Skipping.", sessionId);
            return false;
        }

        return true;
    }
}
