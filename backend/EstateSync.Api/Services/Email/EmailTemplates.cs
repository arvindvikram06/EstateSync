using System.Net;

namespace EstateSync.Api.Services.Email;

public static class EmailTemplates
{
    /// <summary>Branded HTML wrapper for all outbound mail.</summary>
    public static string Render(EmailMessage message)
    {
        var heading = WebUtility.HtmlEncode(message.Heading);
        var body = WebUtility.HtmlEncode(message.Body);

        var cta = string.Empty;
        if (!string.IsNullOrEmpty(message.CtaUrl) && !string.IsNullOrEmpty(message.CtaLabel))
        {
            cta = $"""
                <tr><td style="padding:8px 32px 28px">
                  <a href="{message.CtaUrl}"
                     style="display:inline-block;background:#FF385C;color:#ffffff;text-decoration:none;
                            font-size:14px;font-weight:600;padding:12px 28px;border-radius:40px">
                    {WebUtility.HtmlEncode(message.CtaLabel)}
                  </a>
                </td></tr>
                """;
        }

        return $"""
            <!DOCTYPE html>
            <html>
            <body style="margin:0;padding:0;background:#F7F7F7;font-family:Inter,Roboto,Arial,sans-serif">
              <table role="presentation" width="100%" cellpadding="0" cellspacing="0" style="background:#F7F7F7;padding:32px 12px">
                <tr><td align="center">
                  <table role="presentation" width="560" cellpadding="0" cellspacing="0"
                         style="background:#ffffff;border:1px solid #EBEBEB;border-radius:16px;overflow:hidden">
                    <tr><td style="background:#FF385C;padding:18px 32px">
                      <span style="color:#ffffff;font-size:18px;font-weight:800;letter-spacing:-0.3px">EstateSync</span>
                    </td></tr>
                    <tr><td style="padding:28px 32px 8px">
                      <h1 style="margin:0;font-size:19px;font-weight:700;color:#222222">{heading}</h1>
                    </td></tr>
                    <tr><td style="padding:10px 32px 24px">
                      <p style="margin:0;font-size:14px;line-height:1.7;color:#444444">{body}</p>
                    </td></tr>
                    {cta}
                    <tr><td style="padding:16px 32px;border-top:1px solid #EBEBEB">
                      <p style="margin:0;font-size:12px;color:#717171">
                        You received this email because you have an EstateSync account.
                      </p>
                    </td></tr>
                  </table>
                </td></tr>
              </table>
            </body>
            </html>
            """;
    }

    /// <summary>Plain-text alternative part for clients that don't render HTML.</summary>
    public static string RenderPlainText(EmailMessage message)
    {
        var text = $"{message.Heading}\n\n{message.Body}";
        if (!string.IsNullOrEmpty(message.CtaUrl))
            text += $"\n\n{message.CtaLabel}: {message.CtaUrl}";
        return text;
    }
}
