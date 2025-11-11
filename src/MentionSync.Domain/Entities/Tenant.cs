namespace MentionSync.Domain.Entities;

public class Tenant : EntityBase
{
    public string Name { get; set; } = default!;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public ICollection<AppUser> Users { get; set; } = new List<AppUser>();
    public ICollection<Identity> Identities { get; set; } = new List<Identity>();
    public ICollection<Integration> Integrations { get; set; } = new List<Integration>();
    public ICollection<Channel> Channels { get; set; } = new List<Channel>();
}
