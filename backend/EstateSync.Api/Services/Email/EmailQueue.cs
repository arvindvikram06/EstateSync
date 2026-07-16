using EstateSync.Api.Interfaces.Services.Email;
using Hangfire;

namespace EstateSync.Api.Services.Email;

public class EmailQueue : IEmailQueue
{
    private readonly IBackgroundJobClient _jobs;

    public EmailQueue(IBackgroundJobClient jobs)
    {
        _jobs = jobs;
    }

    public ValueTask EnqueueAsync(EmailMessage message, CancellationToken ct = default)
    {
        _jobs.Enqueue<EmailSenderJob>(j => j.SendAsync(message));
        return ValueTask.CompletedTask;
    }
}
