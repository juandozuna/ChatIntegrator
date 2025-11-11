using MentionSync.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace MentionSync.Infrastructure.Data;

public class MentionSyncDbContext : DbContext
{
    public MentionSyncDbContext(DbContextOptions<MentionSyncDbContext> options) : base(options)
    {
    }

    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<AppUser> AppUsers => Set<AppUser>();
    public DbSet<Identity> Identities => Set<Identity>();
    public DbSet<Integration> Integrations => Set<Integration>();
    public DbSet<Channel> Channels => Set<Channel>();
    public DbSet<SourceMessage> SourceMessages => Set<SourceMessage>();
    public DbSet<Mention> Mentions => Set<Mention>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasPostgresExtension("uuid-ossp");

        modelBuilder.Entity<Tenant>(entity =>
        {
            entity.ToTable("tenant");
            entity.Property(x => x.Name).IsRequired();
            entity.Property(x => x.CreatedAt).HasColumnName("created_at");
        });

        modelBuilder.Entity<AppUser>(entity =>
        {
            entity.ToTable("app_user");
            entity.Property(x => x.Email).IsRequired();
            entity.Property(x => x.CreatedAt).HasColumnName("created_at");
            entity.HasIndex(x => new { x.TenantId, x.Email }).IsUnique();
        });

        modelBuilder.Entity<Identity>(entity =>
        {
            entity.ToTable("identity");
            entity.Property(x => x.Network).IsRequired();
            entity.Property(x => x.ExternalUserId).IsRequired();
            entity.HasIndex(x => new { x.TenantId, x.Network, x.ExternalUserId }).IsUnique();
        });

        modelBuilder.Entity<Integration>(entity =>
        {
            entity.ToTable("integration");
            entity.Property(x => x.Network).IsRequired();
            entity.Property(x => x.ConfigJson).HasColumnName("config").HasColumnType("jsonb");
            entity.Property(x => x.CreatedAt).HasColumnName("created_at");
        });

        modelBuilder.Entity<Channel>(entity =>
        {
            entity.ToTable("channel");
            entity.Property(x => x.Network).IsRequired();
            entity.Property(x => x.ExternalChannelId).IsRequired().HasColumnName("external_channel_id");
            entity.HasIndex(x => new { x.TenantId, x.Network, x.ExternalChannelId }).IsUnique();
        });

        modelBuilder.Entity<SourceMessage>(entity =>
        {
            entity.ToTable("source_message");
            entity.Property(x => x.Network).IsRequired();
            entity.Property(x => x.ExternalMessageId).IsRequired().HasColumnName("external_message_id");
            entity.Property(x => x.Timestamp).HasColumnName("ts");
            entity.Property(x => x.Text).HasColumnName("text");
            entity.Property(x => x.RawJson).HasColumnName("raw").HasColumnType("jsonb");
            entity.Property(x => x.ThreadKey).HasColumnName("thread_key");
            entity.HasIndex(x => new { x.TenantId, x.Network, x.ExternalMessageId }).IsUnique();
        });

        modelBuilder.Entity<Mention>(entity =>
        {
            entity.ToTable("mention");
            entity.Property(x => x.MatchedRule).HasColumnName("matched_rule");
            entity.Property(x => x.IsExplicit).HasColumnName("is_explicit");
            entity.Property(x => x.Confidence).HasColumnName("confidence");
            entity.Property(x => x.CreatedAt).HasColumnName("created_at");
            entity.Property(x => x.Seen).HasColumnName("seen");
            entity.Property(x => x.Priority).HasColumnName("priority");
            entity.Property(x => x.Summary).HasColumnName("summary");
            entity.HasIndex(x => new { x.TenantId, x.Seen, x.Priority, x.CreatedAt }).HasDatabaseName("mention_seen_priority_idx");
        });
    }
}
