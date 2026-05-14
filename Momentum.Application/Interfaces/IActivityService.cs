using Momentum.Shared;

namespace Momentum.Application.Interfaces;

public interface IActivityService
{
    Task<IEnumerable<ActivityDto>> GetAllAsync(string userId);
    Task<IEnumerable<ActivityDto>> GetFrequentAsync(string userId, int count = 10);
    Task<ActivityDto?> GetByIdAsync(int id, string userId);
    Task<ActivityDto> CreateAsync(string userId, CreateActivityDto dto);
    Task<ActivityDto?> UpdateAsync(int id, string userId, UpdateActivityDto dto);
    Task<(bool deleted, int logCount)> DeleteAsync(int id, string userId, string? action);
}
