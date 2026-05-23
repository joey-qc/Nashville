using Momentum.Application.Interfaces;
using Momentum.Domain.Entities;
using Momentum.Shared;

namespace Momentum.Application.Services;

public class ActivityLogService(IActivityLogRepository logRepo) : IActivityLogService
{
    public async Task<IEnumerable<ActivityLogDto>> GetByDateRangeAsync(string userId, DateTime from, DateTime to)
    {
        var logs = await logRepo.GetByDateRangeAsync(userId, from, to);
        return logs.Select(Map);
    }

    public async Task<ActivityLogDto?> GetByIdAsync(int id, string userId)
    {
        var log = await logRepo.GetByIdAsync(id, userId);
        return log is null ? null : Map(log);
    }

    public async Task<ActivityLogDto> CreateAsync(string userId, CreateActivityLogDto dto)
    {
        var log = new ActivityLog
        {
            UserId = userId,
            ActivityId = dto.ActivityId,
            LoggedAt = DateTime.SpecifyKind(dto.LoggedAt, DateTimeKind.Utc),
            PointsRecorded = dto.PointsRecorded,
            Notes = dto.Notes,
            CreatedAt = DateTime.UtcNow
        };

        await logRepo.AddAsync(log);
        await logRepo.SaveChangesAsync();

        var created = await logRepo.GetByIdAsync(log.Id, userId);
        return Map(created!);
    }

    public async Task<ActivityLogDto?> UpdateAsync(int id, string userId, UpdateActivityLogDto dto)
    {
        var log = await logRepo.GetByIdAsync(id, userId);
        if (log is null) return null;

        log.ActivityId = dto.ActivityId;
        log.LoggedAt = DateTime.SpecifyKind(dto.LoggedAt, DateTimeKind.Utc);
        log.PointsRecorded = dto.PointsRecorded;
        log.Notes = dto.Notes;

        await logRepo.SaveChangesAsync();
        return Map(log);
    }

    public async Task<bool> DeleteAsync(int id, string userId)
    {
        var log = await logRepo.GetByIdAsync(id, userId);
        if (log is null) return false;

        await logRepo.DeleteAsync(log);
        await logRepo.SaveChangesAsync();
        return true;
    }

    private static ActivityLogDto Map(ActivityLog l) => new()
    {
        Id = l.Id,
        ActivityId = l.ActivityId,
        ActivityName = l.Activity?.Name ?? string.Empty,
        Categories = l.Activity?.Categories.Select(ac => new CategoryDto
        {
            Id = ac.Category.Id,
            Name = ac.Category.Name,
            ColorHex = ac.Category.ColorHex
        }).ToList() ?? [],
        LoggedAt = DateTime.SpecifyKind(l.LoggedAt, DateTimeKind.Utc),
        PointsRecorded = l.PointsRecorded,
        Notes = l.Notes
    };
}
