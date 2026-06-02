using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Momentum.Domain.Entities;
using Momentum.Infrastructure.Identity;

namespace Momentum.Infrastructure.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options)
    : IdentityDbContext<ApplicationUser>(options)
{
    public DbSet<Dimension>                  Dimensions                  => Set<Dimension>();
    public DbSet<Activity>                   Activities                  => Set<Activity>();
    public DbSet<ActivityDimension>          ActivityDimensions          => Set<ActivityDimension>();
    public DbSet<ActivityLog>                ActivityLogs                => Set<ActivityLog>();
    public DbSet<ActivityLogEntryDimension>  ActivityLogEntryDimensions  => Set<ActivityLogEntryDimension>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // ActivityDimension — composite PK
        builder.Entity<ActivityDimension>()
            .HasKey(ad => new { ad.ActivityId, ad.DimensionId });

        // ActivityDimension → Dimension (Restrict delete)
        builder.Entity<ActivityDimension>()
            .HasOne(ad => ad.Dimension)
            .WithMany(d => d.ActivityDimensions)
            .HasForeignKey(ad => ad.DimensionId)
            .OnDelete(DeleteBehavior.Restrict);

        // Activity → ActivityDimension (Cascade delete)
        builder.Entity<Activity>()
            .HasMany(a => a.Dimensions)
            .WithOne(ad => ad.Activity)
            .HasForeignKey(ad => ad.ActivityId)
            .OnDelete(DeleteBehavior.Cascade);

        // Activity → ActivityLog (Restrict delete)
        builder.Entity<Activity>()
            .HasMany(a => a.Logs)
            .WithOne(l => l.Activity)
            .HasForeignKey(l => l.ActivityId)
            .OnDelete(DeleteBehavior.Restrict);

        // ActivityLogEntryDimension — composite PK
        builder.Entity<ActivityLogEntryDimension>()
            .HasKey(led => new { led.ActivityLogId, led.DimensionId });

        // ActivityLogEntryDimension → ActivityLog (Cascade delete)
        builder.Entity<ActivityLogEntryDimension>()
            .HasOne(led => led.ActivityLog)
            .WithMany(l => l.LogEntryDimensions)
            .HasForeignKey(led => led.ActivityLogId)
            .OnDelete(DeleteBehavior.Cascade);

        // ActivityLogEntryDimension → Dimension (Restrict delete)
        builder.Entity<ActivityLogEntryDimension>()
            .HasOne(led => led.Dimension)
            .WithMany(d => d.LogEntryDimensions)
            .HasForeignKey(led => led.DimensionId)
            .OnDelete(DeleteBehavior.Restrict);

        // Seed data — IDs are stable; names align with user-facing display names
        builder.Entity<Dimension>().HasData(
            new Dimension { Id = 1, Name = "Body",             ColorHex = "#76E04A" },
            new Dimension { Id = 2, Name = "Mind",             ColorHex = "#5BC8FF" },
            new Dimension { Id = 3, Name = "Spirit",           ColorHex = "#B894FF" },
            new Dimension { Id = 4, Name = "Connections",      ColorHex = "#F7B500" },
            new Dimension { Id = 5, Name = "Responsibilities", ColorHex = "#FF9472" }
        );
    }
}
