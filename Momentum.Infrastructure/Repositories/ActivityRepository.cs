using Microsoft.EntityFrameworkCore;
using Momentum.Application.Interfaces;
using Momentum.Domain.Entities;
using Momentum.Infrastructure.Data;

namespace Momentum.Infrastructure.Repositories;

public class ActivityRepository(AppDbContext context) : IActivityRepository
{
    public async Task<IEnumerable<Activity>> GetAllAsync(string userId) =>
        await context.Activities
            .Include(a => a.Dimensions).ThenInclude(ad => ad.Dimension)
            .Where(a => a.UserId == userId && !a.IsArchived)
            .OrderBy(a => a.Name)
            .ToListAsync();

    public async Task<IEnumerable<Activity>> GetFrequentAsync(string userId, int count = 10) =>
        await context.Activities
            .Include(a => a.Dimensions).ThenInclude(ad => ad.Dimension)
            .Where(a => a.UserId == userId && !a.IsArchived)
            .OrderByDescending(a => a.Logs.Count(l => l.UserId == userId))
            .Take(count)
            .ToListAsync();

    public async Task<Activity?> GetByIdAsync(int id, string userId) =>
        await context.Activities
            .Include(a => a.Dimensions).ThenInclude(ad => ad.Dimension)
            .Include(a => a.Logs)
            .FirstOrDefaultAsync(a => a.Id == id && a.UserId == userId);

    public async Task AddAsync(Activity activity) =>
        await context.Activities.AddAsync(activity);

    public Task DeleteAsync(Activity activity)
    {
        context.Activities.Remove(activity);
        return Task.CompletedTask;
    }

    public async Task SaveChangesAsync() =>
        await context.SaveChangesAsync();
}
