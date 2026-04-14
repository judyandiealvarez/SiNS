using Microsoft.EntityFrameworkCore;
using OpenIddict.EntityFrameworkCore.Models;
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
    public DbSet<DomainUpstreamMapping> DomainUpstreamMappings { get; set; }
    public DbSet<DnssecZone> DnssecZones { get; set; }
    public DbSet<OpenIddictEntityFrameworkCoreApplication> OpenIddictApplications { get; set; }
    public DbSet<OpenIddictEntityFrameworkCoreAuthorization> OpenIddictAuthorizations { get; set; }
    public DbSet<OpenIddictEntityFrameworkCoreScope> OpenIddictScopes { get; set; }
    public DbSet<OpenIddictEntityFrameworkCoreToken> OpenIddictTokens { get; set; }

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

        modelBuilder.Entity<DomainUpstreamMapping>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Domain).IsUnique();
            entity.Property(e => e.Domain).IsRequired();
            entity.Property(e => e.UpstreamServer).IsRequired();
        });

        modelBuilder.Entity<DnssecZone>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Apex).IsUnique();
            entity.Property(e => e.Apex).IsRequired();
            entity.Property(e => e.KskPrivateKeyPem).IsRequired();
            entity.Property(e => e.ZskPrivateKeyPem).IsRequired();
        });

        modelBuilder.UseOpenIddict();
    }
}
