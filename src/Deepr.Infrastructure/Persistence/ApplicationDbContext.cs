using Deepr.Domain.Common;
using Deepr.Domain.Entities;
using Deepr.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace Deepr.Infrastructure.Persistence;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Issue> Issues => Set<Issue>();
    public DbSet<Council> Councils => Set<Council>();
    public DbSet<Session> Sessions => Set<Session>();
    public DbSet<SessionRound> SessionRounds => Set<SessionRound>();
    public DbSet<Contribution> Contributions => Set<Contribution>();

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        foreach (var entry in ChangeTracker.Entries<BaseEntity>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedAt = DateTime.UtcNow;
                    break;
                case EntityState.Modified:
                    entry.Entity.UpdatedAt = DateTime.UtcNow;
                    break;
            }
        }

        return base.SaveChangesAsync(cancellationToken);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Issue configuration
        modelBuilder.Entity<Issue>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Title).IsRequired().HasMaxLength(500);
            entity.Property(e => e.ContextVector).IsRequired();
            entity.Property(e => e.OwnerId).IsRequired();
            entity.HasIndex(e => e.OwnerId);
        });

        // Council configuration
        modelBuilder.Entity<Council>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.IssueId).IsRequired();
            entity.Property(e => e.SelectedMethod).IsRequired();
            entity.Property(e => e.SelectedTool).IsRequired();
            entity.HasIndex(e => e.IssueId);

            // Configure CouncilMember as owned entity (value object)
            entity.OwnsMany(c => c.Agents, a =>
            {
                a.Property(m => m.AgentId).IsRequired();
                a.Property(m => m.Name).IsRequired().HasMaxLength(200);
                a.Property(m => m.Role).IsRequired();
                a.Property(m => m.IsAi).IsRequired();
                a.Property(m => m.SystemPromptOverride);
            });
        });

        // Session configuration
        modelBuilder.Entity<Session>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.CouncilId).IsRequired();
            entity.Property(e => e.Status).IsRequired();
            entity.Property(e => e.CurrentRoundNumber).IsRequired();
            entity.Property(e => e.StatePayload).IsRequired().HasColumnType("jsonb");
            entity.HasIndex(e => e.CouncilId);
            entity.HasIndex(e => e.Status);

            entity.HasMany(e => e.Rounds)
                .WithOne()
                .HasForeignKey(sr => sr.SessionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // SessionRound configuration
        modelBuilder.Entity<SessionRound>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.SessionId).IsRequired();
            entity.Property(e => e.RoundNumber).IsRequired();
            entity.Property(e => e.Instructions).IsRequired();
            entity.Property(e => e.Summary);
            entity.HasIndex(e => new { e.SessionId, e.RoundNumber }).IsUnique();

            entity.HasMany(e => e.Contributions)
                .WithOne()
                .HasForeignKey(c => c.SessionRoundId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Contribution configuration
        modelBuilder.Entity<Contribution>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.SessionRoundId).IsRequired();
            entity.Property(e => e.AgentId).IsRequired();
            entity.Property(e => e.RawContent).IsRequired();
            entity.Property(e => e.StructuredData).IsRequired().HasColumnType("jsonb");
            entity.HasIndex(e => e.SessionRoundId);
            entity.HasIndex(e => e.AgentId);
        });
    }
}
