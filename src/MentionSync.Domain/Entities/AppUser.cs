namespace MentionSync.Domain.Entities;

public class AppUser : EntityBase
{
    public Guid TenantId { get; set; }
    public Tenant Tenant { get; set; } = default!;

    public string Email { get; set; } = default!;
    public string? DisplayName { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public ICollection<Identity> LinkedIdentities { get; set; } = new List<Identity>();
}
