using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Momentum.Domain.Entities;
using Momentum.Infrastructure.Identity;

namespace Momentum.Infrastructure.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options)
    : IdentityDbContext<ApplicationUser>(options)
{
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Activity> Activities => Set<Activity>();
    public DbSet<ActivityCategory> ActivityCategories => Set<ActivityCategory>();
    public DbSet<ActivityLog> ActivityLogs => Set<ActivityLog>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<ActivityCategory>()
            .HasKey(ac => new { ac.ActivityId, ac.CategoryId });

        builder.Entity<ActivityCategory>()
            .HasOne(ac => ac.Category)
            .WithMany(c => c.ActivityCategories)
            .HasForeignKey(ac => ac.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Activity>()
            .HasMany(a => a.Categories)
            .WithOne(ac => ac.Activity)
            .HasForeignKey(ac => ac.ActivityId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<Activity>()
            .HasMany(a => a.Logs)
            .WithOne(l => l.Activity)
            .HasForeignKey(l => l.ActivityId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Category>().HasData(
            new Category { Id = 1, Name = "Physical",     ColorHex = "#76E04A" },
            new Category { Id = 2, Name = "Mental",       ColorHex = "#5BC8FF" },
            new Category { Id = 3, Name = "Spiritual",    ColorHex = "#B894FF" },
            new Category { Id = 4, Name = "Social",       ColorHex = "#F7B500" },
            new Category { Id = 5, Name = "Housekeeping", ColorHex = "#FF9472" }
        );
    }
}
