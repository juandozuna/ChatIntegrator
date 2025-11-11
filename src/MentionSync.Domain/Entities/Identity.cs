namespace MentionSync.Domain.Entities;

public class Identity : EntityBase
{
    public Guid TenantId { get; set; }
    public Tenant Tenant { get; set; } = default!;

    public Guid? AppUserId { get; set; }
    public AppUser? AppUser { get; set; }

    public string Network { get; set; } = default!;
    public string ExternalUserId { get; set; } = default!;
    public string? Handle { get; set; }
    public string? Email { get; set; }

    public ICollection<SourceMessage> AuthoredMessages { get; set; } = new List<SourceMessage>();
    public ICollection<Mention> Mentions { get; set; } = new List<Mention>();
}
