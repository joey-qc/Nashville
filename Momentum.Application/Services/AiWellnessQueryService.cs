using Momentum.Application.Interfaces;
using Momentum.Shared;

namespace Momentum.Application.Services;

public class AiWellnessQueryService(IActivityLogRepository logRepo) : IAiWellnessQueryService
{
    public async Task<AiTodayResponseDto> GetTodayAsync(string userId, int? localOffsetMinutes = null)
    {
        var offset = localOffsetMinutes ?? 0;
        // Local-day boundaries derived from the caller's UTC offset — same pattern as
        // ScoreService's daily-totals boundary math (local midnight, converted back to UTC).
        var localToday = DateTime.UtcNow.AddMinutes(offset).Date;
        var fromUtc = localToday.AddMinutes(-offset);
        var toUtc   = localToday.AddDays(1).AddMinutes(-offset);

        var logs = await logRepo.GetByDateRangeAsync(userId, fromUtc, toUtc);

        var entries = logs
            .Select(l => new AiTodayEntryDto
            {
                // EF returns DateTime as Kind=Unspecified; mark Utc explicitly so the API's
                // UtcDateTimeConverter doesn't re-shift it (same fix as KI-017/ActivityLogService).
                LoggedAt     = DateTime.SpecifyKind(l.LoggedAt, DateTimeKind.Utc),
                ActivityName = l.Activity?.Name ?? string.Empty,
                Points       = l.PointsRecorded,
                Dimensions   = l.LogEntryDimensions
                    .Where(led => led.Dimension is not null)
                    .Select(led => led.Dimension.Name)
                    .ToList()
            })
            .ToList();

        return new AiTodayResponseDto
        {
            Date        = DateOnly.FromDateTime(localToday),
            TotalPoints = entries.Sum(e => e.Points),
            EntryCount  = entries.Count,
            Entries     = entries
        };
    }
}
