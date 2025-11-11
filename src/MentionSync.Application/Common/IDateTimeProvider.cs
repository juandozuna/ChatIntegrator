namespace MentionSync.Application.Common;

public interface IDateTimeProvider
{
    DateTimeOffset UtcNow { get; }
}
