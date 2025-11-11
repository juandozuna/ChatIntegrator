using MentionSync.Application.Common;
using MentionSync.Application.Mentions;
using MentionSync.Domain.Entities;
using MentionSync.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace MentionSync.Infrastructure.Services;

public class MentionService : IMentionService
{
    private readonly MentionSyncDbContext _dbContext;

    public MentionService(MentionSyncDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<IReadOnlyCollection<MentionDto>>> ListMentionsAsync(Guid tenantId, bool? seen, int? minPriority, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Mentions
            .AsNoTracking()
            .Include(m => m.SourceMessage)
                .ThenInclude(sm => sm.Channel)
            .Include(m => m.SourceMessage)
                .ThenInclude(sm => sm.AuthorIdentity)
            .Where(m => m.TenantId == tenantId)
            .OrderByDescending(m => m.Priority)
            .ThenByDescending(m => m.CreatedAt);

        if (seen.HasValue)
        {
            query = query.Where(m => m.Seen == seen.Value);
        }

        if (minPriority.HasValue)
        {
            query = query.Where(m => m.Priority >= minPriority.Value);
        }

        var mentions = await query.Take(200).Select(m => new MentionDto(
            m.Id,
            m.SourceMessage.Network,
            m.SourceMessage.Channel?.Name ?? "Unknown",
            m.Summary,
            m.SourceMessage.Text,
            m.Priority,
            m.Seen,
            m.CreatedAt,
            m.SourceMessage.ThreadKey,
            m.SourceMessage.AuthorIdentity?.Handle ?? m.SourceMessage.AuthorIdentity?.Email
        )).ToListAsync(cancellationToken);

        return Result<IReadOnlyCollection<MentionDto>>.Ok(mentions);
    }

    public async Task<Result<MentionDto>> GetMentionAsync(Guid tenantId, Guid mentionId, CancellationToken cancellationToken = default)
    {
        var mention = await _dbContext.Mentions
            .AsNoTracking()
            .Include(m => m.SourceMessage)
                .ThenInclude(sm => sm.Channel)
            .Include(m => m.SourceMessage)
                .ThenInclude(sm => sm.AuthorIdentity)
            .FirstOrDefaultAsync(m => m.TenantId == tenantId && m.Id == mentionId, cancellationToken);

        if (mention is null)
        {
            return Result<MentionDto>.Fail("Mention not found");
        }

        var dto = new MentionDto(
            mention.Id,
            mention.SourceMessage.Network,
            mention.SourceMessage.Channel?.Name ?? "Unknown",
            mention.Summary,
            mention.SourceMessage.Text,
            mention.Priority,
            mention.Seen,
            mention.CreatedAt,
            mention.SourceMessage.ThreadKey,
            mention.SourceMessage.AuthorIdentity?.Handle ?? mention.SourceMessage.AuthorIdentity?.Email
        );

        return Result<MentionDto>.Ok(dto);
    }

    public async Task<Result> MarkSeenAsync(Guid tenantId, Guid mentionId, CancellationToken cancellationToken = default)
    {
        var mention = await _dbContext.Mentions.FirstOrDefaultAsync(m => m.TenantId == tenantId && m.Id == mentionId, cancellationToken);
        if (mention is null)
        {
            return Result.Fail("Mention not found");
        }

        mention.Seen = true;
        await _dbContext.SaveChangesAsync(cancellationToken);
        return Result.Ok();
    }

    public async Task<Result<Mention>> RecordMentionAsync(Mention mention, CancellationToken cancellationToken = default)
    {
        _dbContext.Mentions.Add(mention);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return Result<Mention>.Ok(mention);
    }
}
