using Momentum.Domain.Entities;

namespace Momentum.Application.Interfaces;

public interface IActivityLogRepository
{
    Task<IEnumerable<ActivityLog>> GetByDateRangeAsync(string userId, DateTime from, DateTime to);
    Task<ActivityLog?> GetByIdAsync(int id, string userId);
    Task<int> CountByActivityAsync(int activityId, string userId);
    Task AddAsync(ActivityLog log);
    Task DeleteAsync(ActivityLog log);
    Task DeleteByActivityAsync(int activityId, string userId);
    Task SaveChangesAsync();
}
