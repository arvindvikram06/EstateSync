using System.Threading;
using System.Threading.Tasks;
using EstateSync.Api.Interfaces.Services.Payments;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Stripe;

namespace EstateSync.Api.Services.Payments;

public class StripeWebhookService : IStripeWebhookService
{
    private readonly ITenantPaymentService _tenantPaymentService;
    private readonly IStripeConnectedAccountService _connectedAccountService;
    private readonly IConfiguration _config;
    private readonly ILogger<StripeWebhookService> _logger;

    public StripeWebhookService(
        ITenantPaymentService tenantPaymentService,
        IStripeConnectedAccountService connectedAccountService,
        IConfiguration config,
        ILogger<StripeWebhookService> logger)
    {
        _tenantPaymentService = tenantPaymentService;
        _connectedAccountService = connectedAccountService;
        _config = config;
        _logger = logger;
    }

    public async Task HandleAsync(string json, string stripeSignature, CancellationToken ct = default)
    {
        var webhookSecret           = _config["Stripe:WebhookSecret"];
        var connectedAccountSecret  = _config["Stripe:ConnectedAccountWebhookSecret"];

        Event stripeEvent;
        try
        {
            stripeEvent = EventUtility.ConstructEvent(json, stripeSignature, webhookSecret);
        }
        catch (StripeException)
        {
            stripeEvent = EventUtility.ConstructEvent(json, stripeSignature, connectedAccountSecret);
        }

        _logger.LogDebug("Stripe event received: {EventType} (EventId={EventId})", stripeEvent.Type, stripeEvent.Id);

        switch (stripeEvent.Type)
        {
            case "checkout.session.completed":
                if (stripeEvent.Data.Object is Stripe.Checkout.Session completedSession)
                    await _tenantPaymentService.HandleCheckoutCompletedAsync(completedSession.ToJson(), ct);
                break;

            // Fires when a checkout session expires without a completed payment (24h timeout).
            // The bill remains Pending — we only send a notification to prompt the tenant to retry.
            case "checkout.session.expired":
                if (stripeEvent.Data.Object is Stripe.Checkout.Session expiredSession)
                    await _tenantPaymentService.HandleSessionExpiredAsync(expiredSession.ToJson(), ct);
                break;

            case "account.updated":
                if (stripeEvent.Data.Object is Account account)
                    await _connectedAccountService.HandleAccountUpdatedAsync(account.Id, ct);
                break;

            case "payout.paid":
                if (stripeEvent.Data.Object is Payout payout && !string.IsNullOrEmpty(stripeEvent.Account))
                    await _connectedAccountService.HandlePayoutPaidAsync(payout.ToJson(), stripeEvent.Account, ct);
                break;

            default:
                _logger.LogDebug("Unhandled Stripe event type: {Type}", stripeEvent.Type);
                break;
        }
    }
}
