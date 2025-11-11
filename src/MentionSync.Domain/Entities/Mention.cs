namespace MentionSync.Domain.Entities;

public class Mention : EntityBase
{
    public Guid TenantId { get; set; }
    public Tenant Tenant { get; set; } = default!;

    public Guid SourceMessageId { get; set; }
    public SourceMessage SourceMessage { get; set; } = default!;

    public Guid? MentionedIdentityId { get; set; }
    public Identity? MentionedIdentity { get; set; }

    public string MatchedRule { get; set; } = default!;
    public bool IsExplicit { get; set; }
    public float Confidence { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public bool Seen { get; set; }
    public int Priority { get; set; }
    public string? Summary { get; set; }
}
