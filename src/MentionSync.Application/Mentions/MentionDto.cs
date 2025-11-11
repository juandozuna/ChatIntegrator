namespace MentionSync.Application.Mentions;

public record MentionDto(
    Guid Id,
    string Network,
    string ChannelName,
    string? Summary,
    string? Text,
    int Priority,
    bool Seen,
    DateTimeOffset CreatedAt,
    string? ThreadKey,
    string? AuthorDisplayName
);
