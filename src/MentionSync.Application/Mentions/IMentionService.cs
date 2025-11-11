using MentionSync.Domain.Entities;
using MentionSync.Application.Common;

namespace MentionSync.Application.Mentions;

public interface IMentionService
{
    Task<Result<IReadOnlyCollection<MentionDto>>> ListMentionsAsync(Guid tenantId, bool? seen, int? minPriority, CancellationToken cancellationToken = default);
    Task<Result<MentionDto>> GetMentionAsync(Guid tenantId, Guid mentionId, CancellationToken cancellationToken = default);
    Task<Result> MarkSeenAsync(Guid tenantId, Guid mentionId, CancellationToken cancellationToken = default);
    Task<Result<Mention>> RecordMentionAsync(Mention mention, CancellationToken cancellationToken = default);
}
