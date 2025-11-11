using System.Text.Json;
using MentionSync.Infrastructure.Integrations;
using Microsoft.AspNetCore.Mvc;

namespace MentionSync.API.Controllers;

[ApiController]
[Route("webhooks/gchat")]
public class GoogleChatWebhookController : ControllerBase
{
    private readonly GoogleChatWebhookService _service;

    public GoogleChatWebhookController(GoogleChatWebhookService service)
    {
        _service = service;
    }

    [HttpPost]
    public async Task<IActionResult> ReceiveAsync([FromBody] JsonElement payload, CancellationToken cancellationToken)
    {
        using var document = JsonDocument.Parse(payload.GetRawText());
        await _service.HandleEventAsync(document, cancellationToken);
        return Ok();
    }
}
