using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EstateSync.Api.Interfaces.Services.Payments;
using EstateSync.Api.Models;
using EstateSync.Api.Repositories.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Stripe;
using Stripe.Checkout;
using EstateSync.Api.Exceptions;

namespace EstateSync.Api.Services.Payments;

public class PaymentService : IPaymentService
{
    private readonly IUserRepository _userRepo;
    private readonly ILeaseBillRepository _leaseBillRepo;
    private readonly IStripeConnectedAccountRepository _stripeConnectedAccountRepo;
    private readonly IStripeCustomerProfileRepository _stripeCustomerProfileRepo;
    private readonly IPaymentRepository _paymentRepo;
    private readonly IOwnerSettlementRepository _ownerSettlementRepo;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IConfiguration _config;
    private readonly ILogger<PaymentService> _logger;

    public PaymentService(
        IUserRepository userRepo,
        ILeaseBillRepository leaseBillRepo,
        IStripeConnectedAccountRepository stripeConnectedAccountRepo,
        IStripeCustomerProfileRepository stripeCustomerProfileRepo,
        IPaymentRepository paymentRepo,
        IOwnerSettlementRepository ownerSettlementRepo,
        IUnitOfWork unitOfWork,
        IConfiguration config,
        ILogger<PaymentService> logger)
    {
        _userRepo = userRepo ?? throw new ArgumentNullException(nameof(userRepo));
        _leaseBillRepo = leaseBillRepo ?? throw new ArgumentNullException(nameof(leaseBillRepo));
        _stripeConnectedAccountRepo = stripeConnectedAccountRepo ?? throw new ArgumentNullException(nameof(stripeConnectedAccountRepo));
        _stripeCustomerProfileRepo = stripeCustomerProfileRepo ?? throw new ArgumentNullException(nameof(stripeCustomerProfileRepo));
        _paymentRepo = paymentRepo ?? throw new ArgumentNullException(nameof(paymentRepo));
        _ownerSettlementRepo = ownerSettlementRepo ?? throw new ArgumentNullException(nameof(ownerSettlementRepo));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<string> CreateOwnerOnboardingLinkAsync(Guid ownerId, CancellationToken ct = default)
    {
        var owner = await _userRepo.GetByIdAsync(ownerId);
        if (owner == null) throw new NotFoundException("User", ownerId);

        string stripeAccountId;
        var connectedAccount = await _stripeConnectedAccountRepo.GetByOwnerIdAsync(ownerId);

        // 1. Create the Stripe Connect Express account if it doesn't exist
        if (connectedAccount == null)
        {
            var nameParts = owner.FullName.Split(' ', 2);
            var firstName = nameParts.Length > 0 ? nameParts[0] : owner.FullName;
            var lastName = nameParts.Length > 1 ? nameParts[1] : "";

            var options = new AccountCreateOptions
            {
                Type = "express",
                Country = "US",
                Email = owner.Email,
                BusinessType = "individual",
                BusinessProfile = new AccountBusinessProfileOptions
                {
                    ProductDescription = "Real estate property owner receiving rent.",
                    Url = "https://estatesync.com"
                },
                Individual = new AccountIndividualOptions
                {
                    FirstName = firstName,
                    LastName = lastName,
                    Email = owner.Email
                },
                Capabilities = new AccountCapabilitiesOptions
                {
                    Transfers = new AccountCapabilitiesTransfersOptions { Requested = true }
                }
            };
            var service = new AccountService();
            var account = await service.CreateAsync(options, cancellationToken: ct);

            stripeAccountId = account.Id;

            await _stripeConnectedAccountRepo.AddAsync(new StripeConnectedAccount
            {
                OwnerId = owner.UserId,
                StripeAccountId = stripeAccountId
            });
            await _unitOfWork.SaveChangesAsync(ct);
        }
        else
        {
            stripeAccountId = connectedAccount.StripeAccountId;
        }

        // 2. Generate the onboarding URL
        var linkOptions = new AccountLinkCreateOptions
        {
            Account = stripeAccountId,
            RefreshUrl = "http://localhost:5118/api/v1/payments/refresh",
            ReturnUrl = "http://localhost:5118/api/v1/payments/success",
            Type = "account_onboarding",
        };
        var linkService = new AccountLinkService();
        var accountLink = await linkService.CreateAsync(linkOptions, cancellationToken: ct);

        return accountLink.Url;
    }

    public async Task<string> CreateTenantCheckoutSessionAsync(Guid leaseBillId, Guid tenantId, CancellationToken ct = default)
    {
        var bill = await _leaseBillRepo.GetByIdWithLeaseAndPropertyAsync(leaseBillId);
        if (bill == null) throw new NotFoundException("LeaseBill", leaseBillId);

        // Find Owner's Stripe ID to route the money
        var ownerAccount = await _stripeConnectedAccountRepo.GetByOwnerIdAsync(bill.Lease!.Property!.OwnerId);
        if (ownerAccount == null) throw new BadRequestException("Owner has not setup payments yet.");

        var tenant = await _userRepo.GetByIdAsync(tenantId);
        if (tenant == null) throw new NotFoundException("User", tenantId);

        string stripeCustomerId;
        var stripeCustomer = await _stripeCustomerProfileRepo.GetByTenantIdAsync(tenantId);

        // Create Stripe Customer for Tenant if they don't have one
        if (stripeCustomer == null)
        {
            var customerOptions = new CustomerCreateOptions { Email = tenant.Email, Name = tenant.FullName };
            var customerService = new CustomerService();
            var customer = await customerService.CreateAsync(customerOptions, cancellationToken: ct);
            
            stripeCustomerId = customer.Id;
            await _stripeCustomerProfileRepo.AddAsync(new StripeCustomerProfile
            {
                TenantId = tenantId,
                StripeCustomerId = stripeCustomerId
            });
            await _unitOfWork.SaveChangesAsync(ct);
        }
        else
        {
            stripeCustomerId = stripeCustomer.StripeCustomerId;
        }

       
        double stripePercent = 0.054;
        double stripeFixedInInr = 25.0;

        double rentAmount = (double)bill.Amount;
        double totalCharged = (rentAmount + stripeFixedInInr) / (1.0 - stripePercent);

        long totalChargedInCents = (long)Math.Round(totalCharged * 100);
        long originalRentInCents = (long)Math.Round(rentAmount * 100);
        
        long stripeFeeInCents = totalChargedInCents - originalRentInCents;
        long platformFeeInCents = (long)Math.Round(rentAmount * 0.02 * 100);
        
        long totalApplicationFeeInCents = platformFeeInCents + stripeFeeInCents;

        // Create Checkout Session
        var options = new SessionCreateOptions
        {
            Customer = stripeCustomerId,
            PaymentMethodTypes = new List<string> { "card" },
            LineItems = new List<SessionLineItemOptions>
            {
                new SessionLineItemOptions
                {
                    PriceData = new SessionLineItemPriceDataOptions
                    {
                        UnitAmount = totalChargedInCents, // Charge tenant the total (Rent + Stripe Fee)
                        Currency = "inr",
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
                ApplicationFeeAmount = totalApplicationFeeInCents, // Platform keeps fee + processing cost
                TransferData = new SessionPaymentIntentDataTransferDataOptions
                {
                    Destination = ownerAccount.StripeAccountId, // Owner gets Rent - 2% Platform Fee
                },
            },
            Metadata = new Dictionary<string, string>
            {
                { "LeaseBillId", bill.LeaseBillId.ToString() },
                { "TenantId", tenantId.ToString() }
            },
            SuccessUrl = "http://localhost:5118/api/v1/payments/checkout-success",
            CancelUrl = "http://localhost:5118/api/v1/payments/checkout-cancel",
        };

        var service = new SessionService();
        var session = await service.CreateAsync(options, cancellationToken: ct);

        return session.Url;
    }

    public async Task HandleWebhookAsync(string json, string stripeSignature, CancellationToken ct = default)
    {
        var webhookSecret = _config["Stripe:WebhookSecret"];
        var stripeEvent = EventUtility.ConstructEvent(json, stripeSignature, webhookSecret);

        if (stripeEvent.Type == "checkout.session.completed")
        {
            var session = stripeEvent.Data.Object as Session;
            if (session != null && session.PaymentStatus == "paid")
            {
                var leaseBillId = Guid.Parse(session.Metadata["LeaseBillId"]);
                var tenantId = Guid.Parse(session.Metadata["TenantId"]);

                // 1. Mark Bill as Paid
                var bill = await _leaseBillRepo.GetByIdAsync(leaseBillId);
                if (bill != null)
                {
                    bill.PendingAmount = 0m;
                    // await _leaseBillRepo.UpdateAsync(bill);
                }

                // 2. Record Payment
                await _paymentRepo.AddAsync(new Payment
                {
                    PaymentId = Guid.NewGuid(),
                    LeaseBillId = leaseBillId,
                    PaidByUserId = tenantId,
                    Amount = (session.AmountTotal ?? 0) / 100m,
                    PlatformFee = bill != null ? bill.Amount * 0.02m : ((session.AmountTotal ?? 0) * 0.02m) / 100m,
                    TransactionId = session.PaymentIntentId ?? session.Id,
                    GatewayResponse = "{\"status\": \"Success via Stripe\"}",
                    PaymentStatus = PaymentStatus.Paid,
                    PaidDate = DateTime.UtcNow
                });

                await _unitOfWork.SaveChangesAsync(ct);
                _logger.LogInformation($"Payment processed successfully for Bill {leaseBillId}");
            }
        }
        else if (stripeEvent.Type == "payout.paid")
        {
            var payout = stripeEvent.Data.Object as Payout;
            if (payout != null)
            {
                var connectedAccountId = stripeEvent.Account;
                if (!string.IsNullOrEmpty(connectedAccountId))
                {
                    var connectedAccount = await _stripeConnectedAccountRepo.GetByStripeAccountIdAsync(connectedAccountId);
                    
                    if (connectedAccount != null)
                    {
                        await _ownerSettlementRepo.AddAsync(new OwnerSettlement
                        {
                            OwnerSettlementId = Guid.NewGuid(),
                            OwnerId = connectedAccount.OwnerId,
                            StripePayoutId = payout.Id,
                            Amount = payout.Amount / 100m,
                            Currency = payout.Currency,
                            DestinationId = payout.DestinationId ?? "N/A",
                            ArrivalDate = payout.ArrivalDate,
                            Status = payout.Status
                        });
                        await _unitOfWork.SaveChangesAsync(ct);
                        _logger.LogInformation($"Settlement processed successfully for Owner {connectedAccount.OwnerId}");
                    }
                }
            }
        }
    }
}
