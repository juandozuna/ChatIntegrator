using System.Text.Json;
using MentionSync.Infrastructure.Integrations;
using Microsoft.AspNetCore.Mvc;

namespace MentionSync.API.Controllers;

[ApiController]
[Route("webhooks/msgraph")]
public class TeamsWebhookController : ControllerBase
{
    private readonly TeamsWebhookService _service;

    public TeamsWebhookController(TeamsWebhookService service)
    {
        _service = service;
    }

    [HttpPost]
    public async Task<IActionResult> ReceiveAsync([FromBody] JsonElement payload, CancellationToken cancellationToken)
    {
        using var document = JsonDocument.Parse(payload.GetRawText());
        await _service.HandleChangeNotificationAsync(document, cancellationToken);
        return Ok();
    }
}
