using System.Text.Json;
using MentionSync.Domain;
using MentionSync.Domain.Entities;
using MentionSync.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace MentionSync.Infrastructure.Integrations;

public class GoogleChatWebhookService
{
    private readonly MentionSyncDbContext _dbContext;
    private readonly ILogger<GoogleChatWebhookService> _logger;

    public GoogleChatWebhookService(MentionSyncDbContext dbContext, ILogger<GoogleChatWebhookService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task HandleEventAsync(JsonDocument payload, CancellationToken cancellationToken = default)
    {
        var message = payload.RootElement.GetProperty("message");
        var tenantId = Guid.Parse(message.GetProperty("sender").GetProperty("name").GetString()!.Split('/').Last());
        var messageName = message.GetProperty("name").GetString();
        var text = message.GetProperty("text").GetString();
        var space = message.GetProperty("space").GetProperty("name").GetString();

        var sourceMessage = new SourceMessage
        {
            TenantId = tenantId,
            Network = Networks.GoogleChat,
            ExternalMessageId = messageName!,
            Timestamp = DateTimeOffset.Parse(message.GetProperty("createTime").GetString()!),
            Text = text,
            RawJson = payload.RootElement.ToString(),
            ThreadKey = message.TryGetProperty("thread", out var thread) ? thread.GetProperty("name").GetString() : null
        };

        var channel = await _dbContext.Channels.FirstOrDefaultAsync(c => c.TenantId == tenantId && c.Network == Networks.GoogleChat && c.ExternalChannelId == space, cancellationToken);
        if (channel is null)
        {
            channel = new Channel
            {
                TenantId = tenantId,
                Network = Networks.GoogleChat,
                ExternalChannelId = space!,
                Name = space
            };
            _dbContext.Channels.Add(channel);
        }
        sourceMessage.Channel = channel;

        _dbContext.SourceMessages.Add(sourceMessage);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
