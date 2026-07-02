using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EstateSync.Api.DTOs.Leases;
using EstateSync.Api.Exceptions;
using EstateSync.Api.Interfaces.Services.Notifications;
using EstateSync.Api.Interfaces.Services.Payments;
using EstateSync.Api.Models;
using EstateSync.Api.Repositories.Interfaces;
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

        if (bill.PendingAmount <= 0)
            throw new ConflictException("This bill has already been fully paid.");

        var ownerAccount = await _stripeAccountRepo.GetByOwnerIdAsync(bill.Lease!.Property!.OwnerId);
        if (ownerAccount == null) throw new BadRequestException("Owner has not set up payments yet.");

        var tenant = await _userRepo.GetByIdAsync(tenantId);
        if (tenant == null) throw new NotFoundException("User", tenantId);

        var stripeCustomerId = await GetOrCreateStripeCustomerAsync(tenant, tenantId, ct);

        var (totalCents, rentCents, appFeeCents) = CalculateAmounts(bill.Amount);

        // Embed metadata on both Session AND PaymentIntent so any payment-level
        // webhook (payment_intent.payment_failed, etc.) can correlate back to our bill.
        var correlationMetadata = new Dictionary<string, string>
        {
            { "LeaseBillId", bill.LeaseBillId.ToString() },
            { "TenantId",    tenantId.ToString() }
        };

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
            SuccessUrl = _config["Stripe:CheckoutSuccessUrl"]
                ?? throw new InvalidOperationException("Stripe:CheckoutSuccessUrl is not configured."),
            CancelUrl  = _config["Stripe:CheckoutCancelUrl"]
                ?? throw new InvalidOperationException("Stripe:CheckoutCancelUrl is not configured."),
        };

        var session = await new SessionService().CreateAsync(options, cancellationToken: ct);
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

        // Idempotency guard 1: skip if this transaction was already recorded.
        // Stripe retries webhooks on non-2xx responses, so the same event can arrive more than once.
        var existingPayment = await _paymentRepo.GetByTransactionIdAsync(transactionId, ct);
        if (existingPayment != null)
        {
            _logger.LogWarning(
                "Duplicate webhook: Payment for TransactionId {TxId} already exists (PaymentId={PaymentId}). Skipping.",
                transactionId, existingPayment.PaymentId);
            return;
        }

        var bill = await _leaseBillRepo.GetByIdWithLeaseAndPropertyAsync(leaseBillId);

        // Idempotency guard 2: skip if the bill was already fully settled.
        if (bill != null && bill.PendingAmount == 0m)
        {
            _logger.LogWarning(
                "Duplicate webhook: Bill {BillId} is already fully paid. Skipping TransactionId {TxId}.",
                leaseBillId, transactionId);
            return;
        }

        if (bill != null)
            bill.PendingAmount = 0m;

        var paidAmount = (session.AmountTotal ?? 0) / 100m;

        await _paymentRepo.AddAsync(new Payment
        {
            PaymentId       = Guid.NewGuid(),
            LeaseBillId     = leaseBillId,
            PaidByUserId    = tenantId,
            Amount          = paidAmount,
            PlatformFee     = bill != null ? bill.Amount * 0.02m : paidAmount * 0.02m,
            TransactionId   = transactionId,
            GatewayResponse = "{\"status\":\"paid\",\"source\":\"stripe_checkout\"}",
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
    // This is the authoritative "tenant abandoned" signal — more reliable than
    // payment_intent.payment_failed which fires on each card decline but the session stays open for retry.
    public async Task HandleSessionExpiredAsync(string sessionJson, CancellationToken ct = default)
    {
        var session = Session.FromJson(sessionJson);

        if (!TryExtractMetadata(session.Metadata, session.Id, out var leaseBillId, out var tenantId))
            return;

        _logger.LogInformation("Checkout session {SessionId} expired without payment for Bill {BillId}.", session.Id, leaseBillId);

        // Bill remains Pending — no state change needed. Just notify the tenant.
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
        await _unitOfWork.SaveChangesAsync(ct);
        return customer.Id;
    }

    private (long totalCents, long rentCents, long appFeeCents) CalculateAmounts(decimal billAmount)
    {
        double stripePercent   = double.TryParse(_config["Stripe:StripeFeePercent"],   out var sp) ? sp : 0.054;
        double stripeFixedInr  = double.TryParse(_config["Stripe:StripeFeeFixedInr"],  out var sf) ? sf : 25.0;
        double platformPercent = double.TryParse(_config["Stripe:PlatformFeePercent"], out var pf) ? pf : 0.02;

        double rent        = (double)billAmount;
        double totalCharged = (rent + stripeFixedInr) / (1.0 - stripePercent);

        long totalCents    = (long)Math.Round(totalCharged * 100);
        long rentCents     = (long)Math.Round(rent * 100);
        long stripFeeCents = totalCents - rentCents;
        long platformCents = (long)Math.Round(rent * platformPercent * 100);
        long appFeeCents   = platformCents + stripFeeCents;

        return (totalCents, rentCents, appFeeCents);
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
