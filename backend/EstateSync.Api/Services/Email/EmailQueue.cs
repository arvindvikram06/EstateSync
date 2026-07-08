using System.Collections.Generic;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using EstateSync.Api.Interfaces.Services.Email;

namespace EstateSync.Api.Services.Email;

/// <summary>
/// In-process producer/consumer queue. Enqueue returns instantly so SMTP
/// latency or outages never slow down or fail a business operation; the
/// EmailDispatcher background service drains it.
/// </summary>
public class EmailQueue : IEmailQueue
{
    private readonly Channel<EmailMessage> _channel =
        Channel.CreateUnbounded<EmailMessage>(new UnboundedChannelOptions { SingleReader = true });

    public ValueTask EnqueueAsync(EmailMessage message, CancellationToken ct = default)
        => _channel.Writer.WriteAsync(message, ct);

    public IAsyncEnumerable<EmailMessage> DequeueAllAsync(CancellationToken ct)
        => _channel.Reader.ReadAllAsync(ct);
}
