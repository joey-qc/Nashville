using Momentum.Application.Interfaces;
using Momentum.Domain.Entities;
using Momentum.Shared;

namespace Momentum.Application.Services;

public class ActivityLogService(IActivityLogRepository logRepo, IActivityRepository activityRepo) : IActivityLogService
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
        var activity = await activityRepo.GetByIdAsync(dto.ActivityId, userId);

        // Use the client's explicit selection if provided; otherwise snapshot the activity's current dimensions.
        var dimensions = dto.DimensionIds is { Count: > 0 }
            ? dto.DimensionIds.Select(id => new ActivityLogEntryDimension { DimensionId = id }).ToList()
            : activity?.Dimensions
                .Select(ad => new ActivityLogEntryDimension { DimensionId = ad.DimensionId })
                .ToList() ?? [];

        var log = new ActivityLog
        {
            UserId = userId,
            ActivityId = dto.ActivityId,
            LoggedAt = DateTime.SpecifyKind(dto.LoggedAt, DateTimeKind.Utc),
            PointsRecorded = dto.PointsRecorded,
            Notes = dto.Notes,
            CreatedAt = DateTime.UtcNow,
            LogEntryDimensions = dimensions
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

        if (dto.DimensionIds is { Count: > 0 })
        {
            // Explicit selection: add/remove only the delta so that dimensions staying in the
            // snapshot are left untouched in EF's identity map (avoids same-PK tracked-entity
            // conflicts when a dimension that was already in the snapshot is re-submitted).
            var newIds      = dto.DimensionIds.ToHashSet();
            var existingIds = log.LogEntryDimensions.Select(led => led.DimensionId).ToHashSet();

            foreach (var led in log.LogEntryDimensions.Where(led => !newIds.Contains(led.DimensionId)).ToList())
                log.LogEntryDimensions.Remove(led);

            foreach (var dimId in newIds.Where(id => !existingIds.Contains(id)))
                log.LogEntryDimensions.Add(new ActivityLogEntryDimension { DimensionId = dimId });
        }
        else if (log.ActivityId != dto.ActivityId)
        {
            // No explicit selection but activity changed — re-derive from the new activity.
            var activity        = await activityRepo.GetByIdAsync(dto.ActivityId, userId);
            var newActivityIds  = (activity?.Dimensions ?? []).Select(ad => ad.DimensionId).ToHashSet();
            var existingIds     = log.LogEntryDimensions.Select(led => led.DimensionId).ToHashSet();

            foreach (var led in log.LogEntryDimensions.Where(led => !newActivityIds.Contains(led.DimensionId)).ToList())
                log.LogEntryDimensions.Remove(led);

            foreach (var dimId in newActivityIds.Where(id => !existingIds.Contains(id)))
                log.LogEntryDimensions.Add(new ActivityLogEntryDimension { DimensionId = dimId });
        }
        // No explicit selection, same activity — keep the existing snapshot unchanged.

        log.ActivityId = dto.ActivityId;
        log.LoggedAt = DateTime.SpecifyKind(dto.LoggedAt, DateTimeKind.Utc);
        log.PointsRecorded = dto.PointsRecorded;
        log.Notes = dto.Notes;

        await logRepo.SaveChangesAsync();

        // Re-fetch so that Dimension nav properties on newly added entries are populated
        // via the repository's ThenInclude chain — same pattern used by CreateAsync.
        var updated = await logRepo.GetByIdAsync(log.Id, userId);
        return Map(updated!);
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
        Categories = l.LogEntryDimensions.Select(led => new CategoryDto
        {
            Id       = led.Dimension.Id,
            Name     = led.Dimension.Name,
            ColorHex = led.Dimension.ColorHex
        }).ToList(),
        LoggedAt = DateTime.SpecifyKind(l.LoggedAt, DateTimeKind.Utc),
        PointsRecorded = l.PointsRecorded,
        Notes = l.Notes
    };
}
