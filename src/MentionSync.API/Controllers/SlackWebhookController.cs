using System.IO;
using System.Text.Json;
using MentionSync.Infrastructure.Integrations;
using Microsoft.AspNetCore.Mvc;

namespace MentionSync.API.Controllers;

[ApiController]
[Route("webhooks/slack")]
public class SlackWebhookController : ControllerBase
{
    private readonly SlackWebhookService _service;

    public SlackWebhookController(SlackWebhookService service)
    {
        _service = service;
    }

    [HttpPost]
    public async Task<IActionResult> ReceiveAsync(CancellationToken cancellationToken)
    {
        using var reader = new StreamReader(Request.Body);
        var body = await reader.ReadToEndAsync(cancellationToken);
        if (!Request.Headers.TryGetValue("X-Slack-Signature", out var signature) ||
            !Request.Headers.TryGetValue("X-Slack-Request-Timestamp", out var timestamp) ||
            !_service.ValidateSignature(signature!, timestamp!, body))
        {
            return Unauthorized();
        }

        using var document = JsonDocument.Parse(body);
        await _service.HandleEventAsync(document, cancellationToken);
        if (document.RootElement.TryGetProperty("challenge", out var challenge))
        {
            return Ok(new { challenge = challenge.GetString() });
        }

        return Ok();
    }
}
