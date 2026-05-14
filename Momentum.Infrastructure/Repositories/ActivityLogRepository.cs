using Microsoft.EntityFrameworkCore;
using Momentum.Application.Interfaces;
using Momentum.Domain.Entities;
using Momentum.Infrastructure.Data;

namespace Momentum.Infrastructure.Repositories;

public class ActivityLogRepository(AppDbContext context) : IActivityLogRepository
{
    public async Task<IEnumerable<ActivityLog>> GetByDateRangeAsync(string userId, DateTime from, DateTime to) =>
        await context.ActivityLogs
            .Include(l => l.Activity)
                .ThenInclude(a => a.Categories)
                    .ThenInclude(ac => ac.Category)
            .Where(l => l.UserId == userId && l.LoggedAt >= from && l.LoggedAt < to)
            .OrderByDescending(l => l.LoggedAt)
            .ToListAsync();

    public async Task<ActivityLog?> GetByIdAsync(int id, string userId) =>
        await context.ActivityLogs
            .Include(l => l.Activity)
                .ThenInclude(a => a.Categories)
                    .ThenInclude(ac => ac.Category)
            .FirstOrDefaultAsync(l => l.Id == id && l.UserId == userId);

    public async Task<int> CountByActivityAsync(int activityId, string userId) =>
        await context.ActivityLogs.CountAsync(l => l.ActivityId == activityId && l.UserId == userId);

    public async Task AddAsync(ActivityLog log) =>
        await context.ActivityLogs.AddAsync(log);

    public Task DeleteAsync(ActivityLog log)
    {
        context.ActivityLogs.Remove(log);
        return Task.CompletedTask;
    }

    public async Task DeleteByActivityAsync(int activityId, string userId)
    {
        var logs = await context.ActivityLogs
            .Where(l => l.ActivityId == activityId && l.UserId == userId)
            .ToListAsync();
        context.ActivityLogs.RemoveRange(logs);
    }

    public async Task SaveChangesAsync() =>
        await context.SaveChangesAsync();
}
