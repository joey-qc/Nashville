using Momentum.Application.Interfaces;
using Momentum.Domain.Entities;
using Momentum.Shared;

namespace Momentum.Application.Services;

public class ActivityService(IActivityRepository activityRepo, IActivityLogRepository logRepo) : IActivityService
{
    public async Task<IEnumerable<ActivityDto>> GetAllAsync(string userId)
    {
        var activities = await activityRepo.GetAllAsync(userId);
        return activities.Select(Map);
    }

    public async Task<IEnumerable<ActivityDto>> GetFrequentAsync(string userId, int count = 10)
    {
        var activities = await activityRepo.GetFrequentAsync(userId, count);
        return activities.Select(Map);
    }

    public async Task<ActivityDto?> GetByIdAsync(int id, string userId)
    {
        var activity = await activityRepo.GetByIdAsync(id, userId);
        return activity is null ? null : Map(activity);
    }

    public async Task<ActivityDto> CreateAsync(string userId, CreateActivityDto dto)
    {
        var now = DateTime.UtcNow;
        var activity = new Activity
        {
            UserId = userId,
            Name = dto.Name,
            Description = dto.Description,
            DefaultPoints = dto.DefaultPoints,
            IsArchived = false,
            CreatedAt = now,
            UpdatedAt = now,
            Dimensions = dto.CategoryIds.Select(id => new ActivityDimension { DimensionId = id }).ToList()
        };

        await activityRepo.AddAsync(activity);
        await activityRepo.SaveChangesAsync();
        return Map(activity);
    }

    public async Task<ActivityDto?> UpdateAsync(int id, string userId, UpdateActivityDto dto)
    {
        var activity = await activityRepo.GetByIdAsync(id, userId);
        if (activity is null) return null;

        activity.Name = dto.Name;
        activity.Description = dto.Description;
        activity.DefaultPoints = dto.DefaultPoints;
        activity.UpdatedAt = DateTime.UtcNow;
        activity.Dimensions = dto.CategoryIds.Select(cid => new ActivityDimension { ActivityId = id, DimensionId = cid }).ToList();

        await activityRepo.SaveChangesAsync();
        return Map(activity);
    }

    public async Task<(bool deleted, int logCount)> DeleteAsync(int id, string userId, string? action)
    {
        var activity = await activityRepo.GetByIdAsync(id, userId);
        if (activity is null) return (false, 0);

        var logCount = await logRepo.CountByActivityAsync(id, userId);

        if (logCount > 0 && action is null)
            return (false, logCount);

        if (action == "archive")
        {
            activity.IsArchived = true;
            activity.UpdatedAt = DateTime.UtcNow;
            await activityRepo.SaveChangesAsync();
        }
        else
        {
            if (logCount > 0)
                await logRepo.DeleteByActivityAsync(id, userId);

            activity.Dimensions.Clear();
            await activityRepo.SaveChangesAsync();

            await activityRepo.DeleteAsync(activity);
            await activityRepo.SaveChangesAsync();
        }

        return (true, logCount);
    }

    private static ActivityDto Map(Activity a) => new()
    {
        Id = a.Id,
        Name = a.Name,
        Description = a.Description,
        DefaultPoints = a.DefaultPoints,
        IsArchived = a.IsArchived,
        Categories = a.Dimensions.Select(ad => new CategoryDto
        {
            Id       = ad.Dimension?.Id       ?? ad.DimensionId,
            Name     = ad.Dimension?.Name     ?? string.Empty,
            ColorHex = ad.Dimension?.ColorHex ?? string.Empty
        }).ToList()
    };
}
