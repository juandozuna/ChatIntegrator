namespace MentionSync.Domain.Entities;

public class Channel : EntityBase
{
    public Guid TenantId { get; set; }
    public Tenant Tenant { get; set; } = default!;

    public string Network { get; set; } = default!;
    public string ExternalChannelId { get; set; } = default!;
    public string? Name { get; set; }

    public ICollection<SourceMessage> Messages { get; set; } = new List<SourceMessage>();
}
