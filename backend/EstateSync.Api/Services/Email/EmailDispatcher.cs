using System;
using System.Threading;
using System.Threading.Tasks;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;
using EstateSync.Api.Common;
using EstateSync.Api.Interfaces.Services.Email;

namespace EstateSync.Api.Services.Email;

/// <summary>
/// Background worker that drains the email queue and delivers via SMTP.
/// Failures are retried, then logged — never propagated: the business
/// operation that queued the mail has long since completed.
/// </summary>
public class EmailDispatcher : BackgroundService
{
    private const int MaxAttempts = 3;

    private readonly IEmailQueue _queue;
    private readonly EmailSettings _settings;
    private readonly ILogger<EmailDispatcher> _logger;

    public EmailDispatcher(IEmailQueue queue, IOptions<EmailSettings> settings, ILogger<EmailDispatcher> logger)
    {
        _queue = queue;
        _settings = settings.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("EmailDispatcher started (Enabled={Enabled}, Host={Host})", _settings.Enabled, _settings.Host);

        await foreach (var message in _queue.DequeueAllAsync(stoppingToken))
        {
            if (!_settings.Enabled)
            {
                _logger.LogInformation("Email disabled — dropping mail to {To}: {Subject}", message.To, message.Subject);
                continue;
            }

            await SendWithRetryAsync(message, stoppingToken);
        }
    }

    private async Task SendWithRetryAsync(EmailMessage message, CancellationToken ct)
    {
        for (var attempt = 1; attempt <= MaxAttempts; attempt++)
        {
            try
            {
                await SendAsync(message, ct);
                _logger.LogInformation("Email sent to {To}: {Subject}", message.To, message.Subject);
                return;
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                return; // shutting down
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Email send attempt {Attempt}/{Max} failed for {To}: {Subject}",
                    attempt, MaxAttempts, message.To, message.Subject);

                if (attempt < MaxAttempts)
                    await Task.Delay(TimeSpan.FromSeconds(5 * attempt), ct);
            }
        }

        _logger.LogError("Giving up on email to {To} after {Max} attempts: {Subject}",
            message.To, MaxAttempts, message.Subject);
    }

    private async Task SendAsync(EmailMessage message, CancellationToken ct)
    {
        var mime = new MimeMessage();
        mime.From.Add(new MailboxAddress(_settings.FromName, _settings.FromAddress));
        mime.To.Add(MailboxAddress.Parse(message.To));
        mime.Subject = message.Subject;
        mime.Body = new BodyBuilder
        {
            HtmlBody = EmailTemplates.Render(message),
            TextBody = EmailTemplates.RenderPlainText(message),
        }.ToMessageBody();

        using var smtp = new SmtpClient();
        await smtp.ConnectAsync(_settings.Host, _settings.Port, SecureSocketOptions.StartTls, ct);
        await smtp.AuthenticateAsync(_settings.Username, _settings.Password, ct);
        await smtp.SendAsync(mime, ct);
        await smtp.DisconnectAsync(true, ct);
    }
}
