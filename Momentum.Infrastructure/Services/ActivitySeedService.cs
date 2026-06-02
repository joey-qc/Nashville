using Momentum.Application.Interfaces;
using Momentum.Domain.Entities;
using Momentum.Infrastructure.Data;

namespace Momentum.Infrastructure.Services;

public class ActivitySeedService(AppDbContext context) : IActivitySeedService
{
    // Dimension IDs match HasData seed: 1=Body, 2=Mind, 3=Spirit, 4=Connections, 5=Responsibilities
    private static readonly (string Name, int Points, int[] CategoryIds)[] _defaults =
    [
        ("Exercise / Gym",          15,  [1]),
        ("Hiking",                  10,  [1]),
        ("Meditation",              10,  [2, 3]),
        ("Reading/Learning",        10,  [2]),
        ("Journaling",              10,  [2, 3]),
        ("Cooking a Healthy Meal",  10,  [1, 5]),
        ("Cleaning / Organizing",    5,  [5]),
        ("Socializing",             10,  [4]),
        ("Calling Family",           5,  [4]),
        ("Travel",                  10,  [2, 3]),
        ("Watching Excessive TV",   -5,  [2]),
        ("Skipping Sleep",          -5,  [1, 2]),
        ("Alcohol / Drinking",      -5,  [1, 2]),
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
