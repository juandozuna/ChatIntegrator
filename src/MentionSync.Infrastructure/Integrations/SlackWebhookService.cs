using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using MentionSync.Domain;
using MentionSync.Domain.Entities;
using MentionSync.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MentionSync.Infrastructure.Integrations;

public class SlackWebhookService
{
    private readonly MentionSyncDbContext _dbContext;
    private readonly ILogger<SlackWebhookService> _logger;
    private readonly SlackOptions _options;

    public SlackWebhookService(MentionSyncDbContext dbContext, ILogger<SlackWebhookService> logger, IOptions<SlackOptions> options)
    {
        _dbContext = dbContext;
        _logger = logger;
        _options = options.Value;
    }

    public bool ValidateSignature(string signature, string timestamp, string body)
    {
        var basestring = $"v0:{timestamp}:{body}";
        var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_options.SigningSecret ?? string.Empty));
        var hash = string.Join("", hmac.ComputeHash(Encoding.UTF8.GetBytes(basestring)).Select(b => b.ToString("x2")));
        return signature.Equals($"v0={hash}", StringComparison.OrdinalIgnoreCase);
    }

    public async Task HandleEventAsync(JsonDocument payload, CancellationToken cancellationToken = default)
    {
        if (!payload.RootElement.TryGetProperty("type", out var type) || type.GetString() == "url_verification")
        {
            return;
        }

        if (!payload.RootElement.TryGetProperty("event", out var eventElement))
        {
            return;
        }

        var tenantId = Guid.Parse(payload.RootElement.GetProperty("team_id").GetString()!);
        var text = eventElement.GetProperty("text").GetString();
        var userId = eventElement.GetProperty("user").GetString();
        var channel = eventElement.GetProperty("channel").GetString();
        var ts = eventElement.GetProperty("ts").GetString();

        var tenant = await _dbContext.Tenants.FirstOrDefaultAsync(t => t.Id == tenantId, cancellationToken);
        if (tenant is null)
        {
            _logger.LogWarning("Unknown tenant {TenantId}", tenantId);
            return;
        }

        var sourceMessage = new SourceMessage
        {
            TenantId = tenantId,
            Network = Networks.Slack,
            ExternalMessageId = ts!,
            Timestamp = DateTimeOffset.FromUnixTimeSeconds(long.Parse(ts!.Split('.')[0])),
            Text = text,
            RawJson = payload.RootElement.ToString(),
            ThreadKey = eventElement.TryGetProperty("thread_ts", out var thread) ? thread.GetString() : null
        };

        var channelEntity = await _dbContext.Channels.FirstOrDefaultAsync(c => c.TenantId == tenantId && c.Network == Networks.Slack && c.ExternalChannelId == channel, cancellationToken);
        if (channelEntity is null)
        {
            channelEntity = new Channel
            {
                TenantId = tenantId,
                Network = Networks.Slack,
                ExternalChannelId = channel,
                Name = channel
            };
            _dbContext.Channels.Add(channelEntity);
        }
        sourceMessage.Channel = channelEntity;

        var identity = await _dbContext.Identities.FirstOrDefaultAsync(i => i.TenantId == tenantId && i.Network == Networks.Slack && i.ExternalUserId == userId, cancellationToken);
        if (identity is null)
        {
            identity = new Identity
            {
                TenantId = tenantId,
                Network = Networks.Slack,
                ExternalUserId = userId,
                Handle = userId
            };
            _dbContext.Identities.Add(identity);
        }
        sourceMessage.AuthorIdentity = identity;

        _dbContext.SourceMessages.Add(sourceMessage);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}

public class SlackOptions
{
    public string? SigningSecret { get; set; }
}
