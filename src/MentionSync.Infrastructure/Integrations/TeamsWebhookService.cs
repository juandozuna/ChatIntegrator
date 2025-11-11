using System.Text.Json;
using MentionSync.Domain;
using MentionSync.Domain.Entities;
using MentionSync.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace MentionSync.Infrastructure.Integrations;

public class TeamsWebhookService
{
    private readonly MentionSyncDbContext _dbContext;
    private readonly ILogger<TeamsWebhookService> _logger;

    public TeamsWebhookService(MentionSyncDbContext dbContext, ILogger<TeamsWebhookService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task HandleChangeNotificationAsync(JsonDocument payload, CancellationToken cancellationToken = default)
    {
        foreach (var value in payload.RootElement.GetProperty("value").EnumerateArray())
        {
            var resourceData = value.GetProperty("resourceData");
            var tenantId = Guid.Parse(resourceData.GetProperty("tenantId").GetString()!);
            var messageId = resourceData.GetProperty("id").GetString()!;
            var channelId = resourceData.TryGetProperty("channelIdentity", out var channelIdentity)
                ? channelIdentity.GetProperty("channelId").GetString()
                : resourceData.GetProperty("chatId").GetString();

            var message = new SourceMessage
            {
                TenantId = tenantId,
                Network = Networks.Teams,
                ExternalMessageId = messageId,
                Timestamp = DateTimeOffset.UtcNow,
                Text = resourceData.GetProperty("body").GetProperty("content").GetString(),
                RawJson = resourceData.ToString(),
                ThreadKey = channelId
            };

            var channel = await _dbContext.Channels.FirstOrDefaultAsync(c => c.TenantId == tenantId && c.Network == Networks.Teams && c.ExternalChannelId == channelId, cancellationToken);
            if (channel is null)
            {
                channel = new Channel
                {
                    TenantId = tenantId,
                    Network = Networks.Teams,
                    ExternalChannelId = channelId!,
                    Name = channelId
                };
                _dbContext.Channels.Add(channel);
            }
            message.Channel = channel;

            _dbContext.SourceMessages.Add(message);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
