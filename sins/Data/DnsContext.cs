using Microsoft.EntityFrameworkCore;
using sins.Models;

namespace sins.Data;

public class DnsContext : DbContext
{
    public DnsContext(DbContextOptions<DnsContext> options) : base(options)
    {
    }

    public DbSet<DnsRecord> DnsRecords { get; set; }
    public DbSet<CacheRecord> CacheRecords { get; set; }
    public DbSet<User> Users { get; set; }
    public DbSet<ServerConfig> ServerConfigs { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<DnsRecord>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.Name, e.Type }).IsUnique();
            entity.Property(e => e.Name).IsRequired();
            entity.Property(e => e.Type).IsRequired();
            entity.Property(e => e.Value).IsRequired();
        });

        modelBuilder.Entity<CacheRecord>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.Name, e.Type });
            entity.HasIndex(e => e.ExpiresAt);
            entity.Property(e => e.Name).IsRequired();
            entity.Property(e => e.Type).IsRequired();
            entity.Property(e => e.Response).IsRequired();
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Username).IsUnique();
            entity.HasIndex(e => e.Email).IsUnique();
            entity.Property(e => e.Username).IsRequired();
            entity.Property(e => e.PasswordHash).IsRequired();
            entity.Property(e => e.Email).IsRequired();
        });

        modelBuilder.Entity<ServerConfig>(entity =>
        {
            entity.HasKey(e => e.Key);
            entity.Property(e => e.Key).IsRequired();
            entity.Property(e => e.Value).IsRequired();
            entity.Property(e => e.UpdatedAt).IsRequired();
            entity.Property(e => e.UpdatedBy).IsRequired();
        });
    }
}
