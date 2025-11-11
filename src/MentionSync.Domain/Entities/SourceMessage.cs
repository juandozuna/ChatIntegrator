namespace MentionSync.Domain.Entities;

public class SourceMessage : EntityBase
{
    public Guid TenantId { get; set; }
    public Tenant Tenant { get; set; } = default!;

    public string Network { get; set; } = default!;
    public string ExternalMessageId { get; set; } = default!;

    public Guid? ChannelId { get; set; }
    public Channel? Channel { get; set; }

    public Guid? AuthorIdentityId { get; set; }
    public Identity? AuthorIdentity { get; set; }

    public DateTimeOffset Timestamp { get; set; }
    public string? Text { get; set; }
    public string RawJson { get; set; } = "{}";
    public string? ThreadKey { get; set; }

    public ICollection<Mention> Mentions { get; set; } = new List<Mention>();
}
