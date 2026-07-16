using EstateSync.Api.Common;
using Hangfire;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;

namespace EstateSync.Api.Services.Email;

[AutomaticRetry(Attempts = 3, DelaysInSeconds = [5, 30, 120])]
public class EmailSenderJob
{
    private readonly EmailSettings _settings;
    private readonly ILogger<EmailSenderJob> _logger;

    public EmailSenderJob(IOptions<EmailSettings> settings, ILogger<EmailSenderJob> logger)
    {
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task SendAsync(EmailMessage message)
    {
        if (!_settings.Enabled)
        {
            _logger.LogInformation("Email disabled — dropping mail to {To}: {Subject}", message.To, message.Subject);
            return;
        }

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
        await smtp.ConnectAsync(_settings.Host, _settings.Port, SecureSocketOptions.StartTls);
        await smtp.AuthenticateAsync(_settings.Username, _settings.Password);
        await smtp.SendAsync(mime);
        await smtp.DisconnectAsync(true);

        _logger.LogInformation("Email sent to {To}: {Subject}", message.To, message.Subject);
    }
}
