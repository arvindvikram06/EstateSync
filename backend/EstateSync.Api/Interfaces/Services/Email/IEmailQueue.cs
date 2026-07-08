using System.Threading;
using System.Threading.Tasks;
using EstateSync.Api.Services.Email;

namespace EstateSync.Api.Interfaces.Services.Email;

public interface IEmailQueue
{
    /// <summary>Enqueue a message for background delivery. Returns immediately.</summary>
    ValueTask EnqueueAsync(EmailMessage message, CancellationToken ct = default);

    /// <summary>Dequeue side — consumed by the EmailDispatcher background service.</summary>
    IAsyncEnumerable<EmailMessage> DequeueAllAsync(CancellationToken ct);
}
