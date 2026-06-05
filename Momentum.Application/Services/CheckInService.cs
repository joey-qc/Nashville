using Momentum.Application.Interfaces;
using Momentum.Domain.Entities;
using Momentum.Shared;

namespace Momentum.Application.Services;

public class CheckInService(
    ICheckInRepository checkInRepo,
    IActivityLogRepository logRepo) : ICheckInService
{
    public async Task<IEnumerable<CheckInDto>> GetByDateRangeAsync(string userId, DateTime from, DateTime to)
    {
        var entities = await checkInRepo.GetByDateRangeAsync(userId, from, to);
        return entities.Select(Map);
    }

    public async Task<CheckInDto?> GetByIdAsync(int id, string userId)
    {
        var entity = await checkInRepo.GetByIdAsync(id, userId);
        return entity is null ? null : Map(entity);
    }

    public async Task<CheckInDto> CreateAsync(string userId, CreateCheckInRequestDto dto)
    {
        ValidateScores(dto.BodyScore, dto.EnergyScore, dto.MoodScore);
        await ValidateActivityLogOwnershipAsync(dto.ActivityLogId, userId);

        var entity = new CheckIn
        {
            UserId        = userId,
            CheckedInAt   = dto.CheckedInAt?.ToUniversalTime() ?? DateTime.UtcNow,
            BodyScore     = dto.BodyScore,
            EnergyScore   = dto.EnergyScore,
            MoodScore     = dto.MoodScore,
            ActivityLogId = dto.ActivityLogId,
            CreatedAt     = DateTime.UtcNow
        };

        await checkInRepo.AddAsync(entity);
        await checkInRepo.SaveChangesAsync();
        return Map(entity);
    }

    public async Task<CheckInDto?> UpdateAsync(int id, string userId, UpdateCheckInRequestDto dto)
    {
        var entity = await checkInRepo.GetByIdAsync(id, userId);
        if (entity is null) return null;

        ValidateScores(dto.BodyScore, dto.EnergyScore, dto.MoodScore);
        await ValidateActivityLogOwnershipAsync(dto.ActivityLogId, userId);

        entity.CheckedInAt   = dto.CheckedInAt.ToUniversalTime();
        entity.BodyScore     = dto.BodyScore;
        entity.EnergyScore   = dto.EnergyScore;
        entity.MoodScore     = dto.MoodScore;
        entity.ActivityLogId = dto.ActivityLogId;

        await checkInRepo.SaveChangesAsync();
        return Map(entity);
    }

    public async Task<bool> DeleteAsync(int id, string userId) =>
        await checkInRepo.DeleteAsync(id, userId);

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static void ValidateScores(int body, int energy, int mood)
    {
        if (body   < -5 || body   > 5) throw new ArgumentException("BodyScore must be between -5 and +5.",   nameof(body));
        if (energy < -5 || energy > 5) throw new ArgumentException("EnergyScore must be between -5 and +5.", nameof(energy));
        if (mood   < -5 || mood   > 5) throw new ArgumentException("MoodScore must be between -5 and +5.",   nameof(mood));
    }

    private async Task ValidateActivityLogOwnershipAsync(int? activityLogId, string userId)
    {
        if (activityLogId is null) return;
        var log = await logRepo.GetByIdAsync(activityLogId.Value, userId);
        if (log is null)
            throw new ArgumentException(
                $"ActivityLog {activityLogId} was not found or does not belong to the current user.",
                nameof(activityLogId));
    }

    private static CheckInDto Map(CheckIn c) => new()
    {
        Id            = c.Id,
        UserId        = c.UserId,
        CheckedInAt   = c.CheckedInAt,
        BodyScore     = c.BodyScore,
        EnergyScore   = c.EnergyScore,
        MoodScore     = c.MoodScore,
        ActivityLogId = c.ActivityLogId,
        CreatedAt     = c.CreatedAt
    };
}
