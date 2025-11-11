using MentionSync.Domain.Entities;

namespace MentionSync.Application.Integrations;

public interface IAiEnricher
{
    Task<AiEnrichmentResult> EnrichAsync(SourceMessage message, string targetUserHandle, CancellationToken cancellationToken = default);
}

public record AiEnrichmentResult(int Priority, string Summary, float Confidence, bool IsImplicit);
