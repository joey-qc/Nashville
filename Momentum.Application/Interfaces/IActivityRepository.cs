using Momentum.Domain.Entities;

namespace Momentum.Application.Interfaces;

public interface IActivityRepository
{
    Task<IEnumerable<Activity>> GetAllAsync(string userId);
    Task<IEnumerable<Activity>> GetFrequentAsync(string userId, int count = 10);
    Task<Activity?> GetByIdAsync(int id, string userId);
    Task AddAsync(Activity activity);
    Task DeleteAsync(Activity activity);
    Task SaveChangesAsync();
}
