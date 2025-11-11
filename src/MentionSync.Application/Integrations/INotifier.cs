using MentionSync.Domain.Entities;

namespace MentionSync.Application.Integrations;

public interface INotifier
{
    Task NotifyAsync(Mention mention, CancellationToken cancellationToken = default);
}
