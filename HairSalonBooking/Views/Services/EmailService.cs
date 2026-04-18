using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;
using System.Text;

namespace HairSalonBooking.Services;

public class EmailService : IEmailService
{
    private readonly EmailSettings _settings;
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<EmailService> _logger;

    public EmailService(
        IOptions<EmailSettings> options,
        IWebHostEnvironment environment,
        ILogger<EmailService> logger)
    {
        _settings = options.Value;
        _environment = environment;
        _logger = logger;
    }

    public async Task SendAsync(string to, string subject, string htmlBody)
    {
        var fromAddress = ResolveFromAddress();

        if (_environment.IsDevelopment())
        {
            await SaveToLocalOutboxAsync(to, subject, htmlBody, fromAddress, "Kopia wiadomości z trybu deweloperskiego");
        }

        if (string.IsNullOrWhiteSpace(_settings.Host) || string.IsNullOrWhiteSpace(fromAddress))
        {
            return;
        }

        var message = new MimeMessage();
        message.From.Add(MailboxAddress.Parse(fromAddress));
        message.To.Add(MailboxAddress.Parse(to));
        message.Subject = subject;

        var builder = new BodyBuilder
        {
            HtmlBody = htmlBody
        };

        message.Body = builder.ToMessageBody();

        using var client = new SmtpClient();

        var socketOption = _settings.UseSsl
            ? SecureSocketOptions.SslOnConnect
            : SecureSocketOptions.StartTlsWhenAvailable;

        await client.ConnectAsync(_settings.Host, _settings.Port, socketOption);

        if (!string.IsNullOrWhiteSpace(_settings.UserName))
        {
            await client.AuthenticateAsync(_settings.UserName, _settings.Password);
        }

        await client.SendAsync(message);
        await client.DisconnectAsync(true);
    }

    private string ResolveFromAddress()
    {
        if (!string.IsNullOrWhiteSpace(_settings.UserName) &&
            _settings.Host.Contains("gmail.com", StringComparison.OrdinalIgnoreCase))
        {
            return _settings.UserName;
        }

        if (!string.IsNullOrWhiteSpace(_settings.From))
        {
            return _settings.From;
        }

        return _settings.UserName;
    }

    private async Task SaveToLocalOutboxAsync(
        string to,
        string subject,
        string htmlBody,
        string? fromAddress,
        string mode)
    {
        var outboxPath = Path.Combine(_environment.ContentRootPath, "output", "emails");
        Directory.CreateDirectory(outboxPath);

        var safeSubject = SanitizeFileName(subject);
        var fileName = $"{DateTime.Now:yyyyMMdd-HHmmssfff}-{safeSubject}.html";
        var filePath = Path.Combine(outboxPath, fileName);

        var content = $$"""
        <!DOCTYPE html>
        <html lang="pl">
        <head>
            <meta charset="utf-8" />
            <title>{{subject}}</title>
            <style>
                body { font-family: Segoe UI, Arial, sans-serif; background: #faf7f2; color: #2f2a2e; padding: 24px; }
                .card { max-width: 760px; margin: 0 auto; background: #ffffff; border-radius: 18px; box-shadow: 0 12px 30px rgba(0,0,0,.08); overflow: hidden; }
                .header { padding: 20px 24px; background: linear-gradient(135deg, #8b5e83, #c9a66b); color: #ffffff; }
                .meta { padding: 16px 24px; border-bottom: 1px solid #eee4ea; font-size: 14px; color: #6f4969; }
                .body { padding: 24px; }
                a { color: #8b5e83; }
            </style>
        </head>
        <body>
            <div class="card">
                <div class="header">
                    <h1 style="margin:0; font-size:24px;">HairSalonBooking</h1>
                </div>
                <div class="meta">
                    <div><strong>Od:</strong> {{fromAddress ?? "(brak)"}}</div>
                    <div><strong>Do:</strong> {{to}}</div>
                    <div><strong>Temat:</strong> {{subject}}</div>
                    <div><strong>Tryb:</strong> {{mode}}</div>
                </div>
                <div class="body">
                    {{htmlBody}}
                </div>
            </div>
        </body>
        </html>
        """;

        await File.WriteAllTextAsync(filePath, content, Encoding.UTF8);
        _logger.LogInformation("Email saved to local outbox: {FilePath}", filePath);
    }

    private static string SanitizeFileName(string value)
    {
        var invalidChars = Path.GetInvalidFileNameChars();
        var sanitized = new string(value
            .Select(ch => invalidChars.Contains(ch) ? '-' : ch)
            .ToArray())
            .Trim();

        return string.IsNullOrWhiteSpace(sanitized) ? "email" : sanitized;
    }
}
