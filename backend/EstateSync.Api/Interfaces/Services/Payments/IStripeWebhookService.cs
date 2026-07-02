using System.Threading;
using System.Threading.Tasks;

namespace EstateSync.Api.Interfaces.Services.Payments;

public interface IStripeWebhookService
{
    Task HandleAsync(string json, string stripeSignature, CancellationToken ct = default);
}
