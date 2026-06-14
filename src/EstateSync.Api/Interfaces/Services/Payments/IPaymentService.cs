using System;
using System.Threading;
using System.Threading.Tasks;

namespace EstateSync.Api.Interfaces.Services.Payments;

public interface IPaymentService
{
    Task<string> CreateOwnerOnboardingLinkAsync(Guid ownerId, CancellationToken ct = default);
    Task<string> CreateTenantCheckoutSessionAsync(Guid leaseBillId, Guid tenantId, CancellationToken ct = default);
    Task HandleWebhookAsync(string json, string stripeSignature, CancellationToken ct = default);
}
