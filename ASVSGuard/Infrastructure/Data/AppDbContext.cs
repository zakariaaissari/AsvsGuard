using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using ASVSGuard.Core.Entities;

namespace ASVSGuard.Infrastructure.Data;

public class AppDbContext : IdentityDbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Exigence> Exigences => Set<Exigence>();
    public DbSet<RepoScan> RepoScans => Set<RepoScan>();
    public DbSet<ScanFinding> ScanFindings => Set<ScanFinding>();
    public DbSet<ChatSession> ChatSessions => Set<ChatSession>();
    public DbSet<ChatMessage> ChatMessages => Set<ChatMessage>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Exigence>(e =>
        {
            e.HasIndex(x => x.Code).IsUnique();
            e.Property(x => x.Code).HasMaxLength(20).IsRequired();
            e.Property(x => x.Category).HasMaxLength(100).IsRequired();
            e.Property(x => x.Group).HasMaxLength(200).IsRequired();
            e.Property(x => x.Description).IsRequired();
            e.Property(x => x.Status)
                .HasConversion<string>()
                .HasMaxLength(20);
        });

        builder.Entity<RepoScan>(e =>
        {
            e.Property(x => x.Status)
                .HasConversion<string>()
                .HasMaxLength(20);
            e.HasMany(x => x.Findings).WithOne(x => x.RepoScan).HasForeignKey(x => x.RepoScanId);
        });

        builder.Entity<ScanFinding>(e =>
        {
            e.Property(x => x.FindingStatus)
                .HasConversion<string>()
                .HasMaxLength(20);
            e.HasOne(x => x.Exigence).WithMany().HasForeignKey(x => x.ExigenceId);
        });

        builder.Entity<ChatSession>(e =>
        {
            e.HasMany(x => x.Messages).WithOne(x => x.Session).HasForeignKey(x => x.SessionId);
        });

        builder.Entity<ChatMessage>(e =>
        {
            e.Property(x => x.Role).HasMaxLength(20).IsRequired();
        });
    }
}
