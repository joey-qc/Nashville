using Momentum.Domain.Entities;

namespace Momentum.Application.Interfaces;

public interface ICheckInRepository
{
    Task<CheckIn?> GetByIdAsync(int id, string userId);
    Task<IEnumerable<CheckIn>> GetByDateRangeAsync(string userId, DateTime from, DateTime to);
    Task AddAsync(CheckIn checkIn);
    Task SaveChangesAsync();
    Task<bool> DeleteAsync(int id, string userId);
}
