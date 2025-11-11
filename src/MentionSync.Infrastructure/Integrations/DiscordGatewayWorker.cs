using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using MentionSync.Domain;
using MentionSync.Domain.Entities;
using MentionSync.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MentionSync.Infrastructure.Integrations;

public class DiscordGatewayWorker : BackgroundService
{
    private readonly DiscordOptions _options;
    private readonly IDbContextFactory<MentionSyncDbContext> _dbContextFactory;
    private readonly ILogger<DiscordGatewayWorker> _logger;

    public DiscordGatewayWorker(IOptions<DiscordOptions> options, IDbContextFactory<MentionSyncDbContext> dbContextFactory, ILogger<DiscordGatewayWorker> logger)
    {
        _options = options.Value;
        _dbContextFactory = dbContextFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (string.IsNullOrWhiteSpace(_options.Token))
        {
            _logger.LogInformation("Discord token not configured; skipping gateway connection");
            return;
        }

        using var client = new ClientWebSocket();
        await client.ConnectAsync(new Uri(_options.GatewayUrl), stoppingToken);

        var buffer = new byte[4096];
        while (!stoppingToken.IsCancellationRequested)
        {
            var result = await client.ReceiveAsync(buffer, stoppingToken);
            if (result.MessageType == WebSocketMessageType.Close)
            {
                break;
            }

            var json = Encoding.UTF8.GetString(buffer, 0, result.Count);
            var document = JsonDocument.Parse(json);
            if (document.RootElement.GetProperty("t").GetString() == "MESSAGE_CREATE")
            {
                await HandleMessageAsync(document.RootElement.GetProperty("d"), stoppingToken);
            }
        }
    }

    private async Task HandleMessageAsync(JsonElement payload, CancellationToken cancellationToken)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        var tenantId = Guid.Parse(payload.GetProperty("guild_id").GetString()!);
        var channelId = payload.GetProperty("channel_id").GetString()!;
        var messageId = payload.GetProperty("id").GetString()!;
        var authorId = payload.GetProperty("author").GetProperty("id").GetString()!;
        var content = payload.GetProperty("content").GetString();

        var message = new SourceMessage
        {
            TenantId = tenantId,
            Network = Networks.Discord,
            ExternalMessageId = messageId,
            Timestamp = DateTimeOffset.UtcNow,
            Text = content,
            RawJson = payload.ToString(),
            ThreadKey = payload.TryGetProperty("thread", out var thread) ? thread.GetProperty("id").GetString() : null
        };

        var channel = await dbContext.Channels.FirstOrDefaultAsync(c => c.TenantId == tenantId && c.Network == Networks.Discord && c.ExternalChannelId == channelId, cancellationToken);
        if (channel is null)
        {
            channel = new Channel
            {
                TenantId = tenantId,
                Network = Networks.Discord,
                ExternalChannelId = channelId,
                Name = channelId
            };
            dbContext.Channels.Add(channel);
        }
        message.Channel = channel;

        var identity = await dbContext.Identities.FirstOrDefaultAsync(i => i.TenantId == tenantId && i.Network == Networks.Discord && i.ExternalUserId == authorId, cancellationToken);
        if (identity is null)
        {
            identity = new Identity
            {
                TenantId = tenantId,
                Network = Networks.Discord,
                ExternalUserId = authorId,
                Handle = payload.GetProperty("author").GetProperty("username").GetString()
            };
            dbContext.Identities.Add(identity);
        }
        message.AuthorIdentity = identity;

        dbContext.SourceMessages.Add(message);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}

public class DiscordOptions
{
    public string GatewayUrl { get; set; } = "wss://gateway.discord.gg/?v=10&encoding=json";
    public string? Token { get; set; }
}
