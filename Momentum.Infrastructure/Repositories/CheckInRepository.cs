using Microsoft.EntityFrameworkCore;
using Momentum.Application.Interfaces;
using Momentum.Domain.Entities;
using Momentum.Infrastructure.Data;

namespace Momentum.Infrastructure.Repositories;

public class CheckInRepository(AppDbContext context) : ICheckInRepository
{
    public async Task<CheckIn?> GetByIdAsync(int id, string userId) =>
        await context.CheckIns
            .Include(c => c.ActivityLog).ThenInclude(l => l!.Activity)
            .FirstOrDefaultAsync(c => c.Id == id && c.UserId == userId);

    public async Task<IEnumerable<CheckIn>> GetByDateRangeAsync(string userId, DateTime from, DateTime to) =>
        await context.CheckIns
            .Include(c => c.ActivityLog).ThenInclude(l => l!.Activity)
            .Where(c => c.UserId == userId && c.CheckedInAt >= from && c.CheckedInAt < to)
            .OrderByDescending(c => c.CheckedInAt)
            .ToListAsync();

    public async Task AddAsync(CheckIn checkIn) =>
        await context.CheckIns.AddAsync(checkIn);

    public async Task SaveChangesAsync() =>
        await context.SaveChangesAsync();

    public async Task<bool> DeleteAsync(int id, string userId)
    {
        var checkIn = await context.CheckIns
            .FirstOrDefaultAsync(c => c.Id == id && c.UserId == userId);
        if (checkIn is null) return false;
        context.CheckIns.Remove(checkIn);
        await context.SaveChangesAsync();
        return true;
    }
}
