using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net.Http.Headers;
using System.Text.Json;

namespace MentionSync.Infrastructure.Services;

public class SubscriptionRenewer
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly GraphOptions _options;
    private readonly ILogger<SubscriptionRenewer> _logger;

    public SubscriptionRenewer(IHttpClientFactory httpClientFactory, IOptions<GraphOptions> options, ILogger<SubscriptionRenewer> logger)
    {
        _httpClientFactory = httpClientFactory;
        _options = options.Value;
        _logger = logger;
    }

    public async Task RenewAsync(CancellationToken cancellationToken = default)
    {
        var client = _httpClientFactory.CreateClient("Graph");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _options.AccessToken);

        var response = await client.PostAsync("/subscriptions", new StringContent(JsonSerializer.Serialize(new
        {
            changeType = "updated",
            notificationUrl = _options.NotificationUrl,
            resource = "/teams/getAllMessages",
            expirationDateTime = DateTime.UtcNow.AddDays(2).ToString("o")
        })), cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("Failed to renew subscription: {Status}", response.StatusCode);
        }
    }
}

public class GraphOptions
{
    public string? AccessToken { get; set; }
    public string? NotificationUrl { get; set; }
}
