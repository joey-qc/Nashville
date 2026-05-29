using Momentum.Application.Interfaces;
using Momentum.Domain.Entities;
using Momentum.Infrastructure.Data;

namespace Momentum.Infrastructure.Services;

public class ActivitySeedService(AppDbContext context) : IActivitySeedService
{
    // Dimension IDs match HasData seed: 1=Physical, 2=Mental, 3=Spiritual, 4=Social, 5=Housekeeping
    private static readonly (string Name, int Points, int[] CategoryIds)[] _defaults =
    [
        ("Exercise / Gym",           8,  [1]),
        ("Hiking (Solo)",            8,  [1]),
        ("Hiking (With Others)",     9,  [1, 4]),
        ("Meditation",               7,  [2, 3]),
        ("Reading (Nonfiction)",     5,  [2]),
        ("Journaling",               6,  [2, 3]),
        ("Cooking a Healthy Meal",   5,  [1, 5]),
        ("Cleaning / Organizing",    4,  [5]),
        ("Socializing with Friends", 6,  [4]),
        ("Calling Family",           5,  [4]),
        ("Travel (Solo)",            7,  [2, 3]),
        ("Travel (With Others)",     9,  [2, 3, 4]),
        ("Watching Excessive TV",   -3,  [2]),
        ("Skipping Sleep",          -6,  [1, 2]),
    ];

    public async Task SeedDefaultActivitiesAsync(string userId)
    {
        var now = DateTime.UtcNow;
        var activities = _defaults.Select(d => new Activity
        {
            UserId = userId,
            Name = d.Name,
            DefaultPoints = d.Points,
            IsArchived = false,
            CreatedAt = now,
            UpdatedAt = now,
            Dimensions = d.CategoryIds.Select(id => new ActivityDimension { DimensionId = id }).ToList()
        }).ToList();

        await context.Activities.AddRangeAsync(activities);
        await context.SaveChangesAsync();
    }
}
