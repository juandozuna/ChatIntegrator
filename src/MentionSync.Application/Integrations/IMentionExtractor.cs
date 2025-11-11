using MentionSync.Domain.Entities;

namespace MentionSync.Application.Integrations;

public interface IMentionExtractor
{
    Task<IReadOnlyCollection<Mention>> ExtractMentionsAsync(SourceMessage message, CancellationToken cancellationToken = default);
}
