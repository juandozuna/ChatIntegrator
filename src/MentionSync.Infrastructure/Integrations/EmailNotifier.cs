using System.Text;
using MailKit.Net.Smtp;
using MailKit.Security;
using MentionSync.Application.Integrations;
using MentionSync.Domain.Entities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;

namespace MentionSync.Infrastructure.Integrations;

public class EmailNotifier : INotifier
{
    private readonly SmtpOptions _options;
    private readonly ILogger<EmailNotifier> _logger;

    public EmailNotifier(IOptions<SmtpOptions> options, ILogger<EmailNotifier> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public async Task NotifyAsync(Mention mention, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(_options.From))
        {
            _logger.LogWarning("SMTP From address is not configured; skipping email notification");
            return;
        }

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress("MentionSync", _options.From));

        if (mention.MentionedIdentity?.Email is { Length: > 0 } email)
        {
            message.To.Add(MailboxAddress.Parse(email));
        }
        else
        {
            _logger.LogInformation("Mention {MentionId} has no email recipient", mention.Id);
            return;
        }

        message.Subject = $"[{mention.Priority}] New mention in {mention.SourceMessage?.Channel?.Name ?? mention.SourceMessage?.Network}";
        var builder = new BodyBuilder
        {
            TextBody = new StringBuilder()
                .AppendLine(mention.SourceMessage?.Text)
                .AppendLine()
                .AppendLine("Summary:")
                .AppendLine(mention.Summary)
                .ToString()
        };
        message.Body = builder.ToMessageBody();

        using var client = new SmtpClient();
        await client.ConnectAsync(_options.Host, _options.Port, SecureSocketOptions.StartTlsWhenAvailable, cancellationToken);

        if (!string.IsNullOrEmpty(_options.Username))
        {
            await client.AuthenticateAsync(_options.Username, _options.Password, cancellationToken);
        }

        await client.SendAsync(message, cancellationToken);
        await client.DisconnectAsync(true, cancellationToken);
    }
}

public class SmtpOptions
{
    public string Host { get; set; } = "";
    public int Port { get; set; } = 587;
    public string? Username { get; set; }
    public string? Password { get; set; }
    public string? From { get; set; }
}
