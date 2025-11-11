namespace MentionSync.Domain.Entities;

public class Integration : EntityBase
{
    public Guid TenantId { get; set; }
    public Tenant Tenant { get; set; } = default!;

    public string Network { get; set; } = default!;
    public string ConfigJson { get; set; } = "{}";
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
