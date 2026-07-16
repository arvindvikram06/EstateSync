using EstateSync.Api.Services.Email;

namespace EstateSync.Api.Interfaces.Services.Email;

public interface IEmailQueue
{
    ValueTask EnqueueAsync(EmailMessage message, CancellationToken ct = default);
}
