using Momentum.Shared;

namespace Momentum.Application.Interfaces;

public interface IActivityLogService
{
    Task<IEnumerable<ActivityLogDto>> GetByDateRangeAsync(string userId, DateTime from, DateTime to);
    Task<ActivityLogDto?> GetByIdAsync(int id, string userId);
    Task<ActivityLogDto> CreateAsync(string userId, CreateActivityLogDto dto);
    Task<ActivityLogDto?> UpdateAsync(int id, string userId, UpdateActivityLogDto dto);
    Task<bool> DeleteAsync(int id, string userId);
}
