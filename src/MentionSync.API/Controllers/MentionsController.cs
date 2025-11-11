using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MentionSync.Application.Mentions;

namespace MentionSync.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class MentionsController : ControllerBase
{
    private readonly IMentionService _mentionService;

    public MentionsController(IMentionService mentionService)
    {
        _mentionService = mentionService;
    }

    [HttpGet]
    public async Task<IActionResult> ListAsync([FromQuery] bool? seen, [FromQuery(Name = "priority")] int? minPriority, CancellationToken cancellationToken)
    {
        var tenantId = GetTenantId();
        var result = await _mentionService.ListMentionsAsync(tenantId, seen, minPriority, cancellationToken);
        if (!result.Success)
        {
            return BadRequest(result.Error);
        }

        return Ok(result.Value);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        var tenantId = GetTenantId();
        var result = await _mentionService.GetMentionAsync(tenantId, id, cancellationToken);
        if (!result.Success)
        {
            return NotFound(result.Error);
        }

        return Ok(result.Value);
    }

    [HttpPost("{id:guid}/seen")]
    public async Task<IActionResult> MarkSeenAsync(Guid id, CancellationToken cancellationToken)
    {
        var tenantId = GetTenantId();
        var result = await _mentionService.MarkSeenAsync(tenantId, id, cancellationToken);
        if (!result.Success)
        {
            return NotFound(result.Error);
        }

        return NoContent();
    }

    private Guid GetTenantId()
    {
        var tenantClaim = User.FindFirst("tenant_id")?.Value;
        return tenantClaim is not null ? Guid.Parse(tenantClaim) : throw new InvalidOperationException("Tenant not provided");
    }
}
