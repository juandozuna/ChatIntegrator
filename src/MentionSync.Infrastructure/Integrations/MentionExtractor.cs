using System.Text.RegularExpressions;
using MentionSync.Application.Integrations;
using MentionSync.Domain.Entities;

namespace MentionSync.Infrastructure.Integrations;

public class MentionExtractor : IMentionExtractor
{
    private static readonly Regex ExplicitMentionRegex = new("<@(?<id>[A-Za-z0-9]+)>", RegexOptions.Compiled);

    public Task<IReadOnlyCollection<Mention>> ExtractMentionsAsync(SourceMessage message, CancellationToken cancellationToken = default)
    {
        var results = new List<Mention>();

        if (string.IsNullOrWhiteSpace(message.Text))
        {
            return Task.FromResult<IReadOnlyCollection<Mention>>(results);
        }

        foreach (Match match in ExplicitMentionRegex.Matches(message.Text))
        {
            results.Add(new Mention
            {
                TenantId = message.TenantId,
                SourceMessageId = message.Id,
                MatchedRule = "explicit-tag",
                IsExplicit = true,
                Confidence = 0.95f,
                Priority = 1
            });
        }

        if (message.Text.Contains("urgent", StringComparison.OrdinalIgnoreCase))
        {
            results.Add(new Mention
            {
                TenantId = message.TenantId,
                SourceMessageId = message.Id,
                MatchedRule = "keyword:urgent",
                IsExplicit = false,
                Confidence = 0.5f,
                Priority = 2
            });
        }

        return Task.FromResult<IReadOnlyCollection<Mention>>(results);
    }
}
