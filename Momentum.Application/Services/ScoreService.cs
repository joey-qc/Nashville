using Momentum.Application.Interfaces;
using Momentum.Shared;

namespace Momentum.Application.Services;

public class ScoreService(IActivityLogRepository logRepo) : IScoreService
{
    public async Task<ScoreSummaryDto> GetSummaryAsync(string userId,
        DateTime? todayStartUtc = null, DateTime? weekStartUtc = null, DateTime? monthStartUtc = null)
    {
        // Prefer client-supplied UTC boundaries (derived from the user's local timezone in
        // the browser) over server UTC.Now.Date, which would use UTC midnight regardless of
        // the user's local timezone and produce wrong day/week/month totals for non-UTC users.
        var now        = DateTime.UtcNow;
        var todayStart = todayStartUtc  ?? now.Date;
        var weekStart  = weekStartUtc   ?? todayStart.AddDays(-(int)now.DayOfWeek);
        var monthStart = monthStartUtc  ?? new DateTime(now.Year, now.Month, 1);

        var monthLogs = await logRepo.GetByDateRangeAsync(userId, monthStart, now.AddDays(1));
        var list = monthLogs.ToList();

        return new ScoreSummaryDto
        {
            TodayTotal = list.Where(l => l.LoggedAt >= todayStart).Sum(l => l.PointsRecorded),
            WeekTotal  = list.Where(l => l.LoggedAt >= weekStart).Sum(l => l.PointsRecorded),
            MonthTotal = list.Sum(l => l.PointsRecorded)
        };
    }

    public async Task<WeeklyComparisonDto> GetWeeklyComparisonAsync(string userId, int? localOffsetMinutes = null)
    {
        var offset = localOffsetMinutes ?? 0;
        // Derive local-day boundaries using the client's UTC offset so the day buckets
        // align with the user's calendar day rather than UTC midnight.
        var localNow      = DateTime.UtcNow.AddMinutes(offset);
        var today         = localNow.Date;
        var thisWeekStart = today.AddDays(-(int)today.DayOfWeek);
        var lastWeekStart = thisWeekStart.AddDays(-7);

        // Convert local week boundaries back to UTC for the repository range query.
        // A 1-day buffer on each side ensures no local day is clipped at the boundary.
        var utcFrom = lastWeekStart.AddMinutes(-offset).AddDays(-1);
        var utcTo   = thisWeekStart.AddDays(7).AddMinutes(-offset).AddDays(1);
        var twoWeeksLogs = await logRepo.GetByDateRangeAsync(userId, utcFrom, utcTo);
        var list = twoWeeksLogs.ToList();

        var days = new[] { "Sun", "Mon", "Tue", "Wed", "Thu", "Fri", "Sat" };

        return new WeeklyComparisonDto
        {
            Days = days.Select((label, i) => new DayComparisonDto
            {
                DayLabel    = label,
                CurrentWeek = list
                    .Where(l => l.LoggedAt.AddMinutes(offset).Date == thisWeekStart.AddDays(i))
                    .Sum(l => l.PointsRecorded),
                LastWeek    = list
                    .Where(l => l.LoggedAt.AddMinutes(offset).Date == lastWeekStart.AddDays(i))
                    .Sum(l => l.PointsRecorded)
            }).ToList()
        };
    }

    public async Task<IEnumerable<DailyScoreDto>> GetDailyTotalsAsync(string userId, int days, int? categoryId, int? localOffsetMinutes = null)
    {
        var offset = localOffsetMinutes ?? 0;
        // Convert the local end-of-today to UTC so the range captures the user's current day.
        var localToday = DateTime.UtcNow.AddMinutes(offset).Date;
        var to   = localToday.AddDays(1).AddMinutes(-offset);
        var from = to.AddDays(-days);
        return await GetTotalsAsync(userId, from, to, "day", categoryId, localOffsetMinutes);
    }

    public async Task<IEnumerable<DailyScoreDto>> GetWeeklyTotalsAsync(string userId, int weeks, int? categoryId)
    {
        var to   = DateTime.UtcNow.Date.AddDays(1);
        var from = to.AddDays(-weeks * 7);
        return await GetTotalsAsync(userId, from, to, "week", categoryId);
    }

    public async Task<IEnumerable<DailyScoreDto>> GetMonthlyTotalsAsync(string userId, int months, int? categoryId)
    {
        var to   = DateTime.UtcNow.Date.AddDays(1);
        var from = to.AddMonths(-months);
        return await GetTotalsAsync(userId, from, to, "month", categoryId);
    }

    private async Task<IEnumerable<DailyScoreDto>> GetTotalsAsync(
        string userId, DateTime from, DateTime to, string groupBy, int? categoryId, int? localOffsetMinutes = null)
    {
        var offset = localOffsetMinutes ?? 0;
        var logs = await logRepo.GetByDateRangeAsync(userId, from, to);

        var filtered = categoryId.HasValue
            ? logs.Where(l => l.LogEntryDimensions.Any(led => led.DimensionId == categoryId.Value))
            : logs;

        return groupBy switch
        {
            "week" => filtered
                .GroupBy(l => GetWeekStart(l.LoggedAt.Date))
                .OrderBy(g => g.Key)
                .Select(g => new DailyScoreDto
                {
                    Date       = DateOnly.FromDateTime(g.Key),
                    Total      = g.Sum(l => l.PointsRecorded),
                    ByCategory = BuildByCategory(g, categoryId)
                }),
            "month" => filtered
                .GroupBy(l => new DateTime(l.LoggedAt.Year, l.LoggedAt.Month, 1))
                .OrderBy(g => g.Key)
                .Select(g => new DailyScoreDto
                {
                    Date       = DateOnly.FromDateTime(g.Key),
                    Total      = g.Sum(l => l.PointsRecorded),
                    ByCategory = BuildByCategory(g, categoryId)
                }),
            // Daily grouping: use client's local day rather than UTC date so entries
            // logged in the late evening appear under the correct local calendar day.
            _ => filtered
                .GroupBy(l => l.LoggedAt.AddMinutes(offset).Date)
                .OrderBy(g => g.Key)
                .Select(g => new DailyScoreDto
                {
                    Date       = DateOnly.FromDateTime(g.Key),
                    Total      = g.Sum(l => l.PointsRecorded),
                    ByCategory = BuildByCategory(g, categoryId)
                })
        };
    }

    public async Task<IEnumerable<CategoryTotalDto>> GetCategoryTotalsAsync(string userId, string period)
    {
        var now = DateTime.UtcNow;
        var (from, to) = period switch
        {
            "month" => (new DateTime(now.Year, now.Month, 1), now.Date.AddDays(1)),
            "year"  => (new DateTime(now.Year, 1, 1),         now.Date.AddDays(1)),
            _       => (now.Date.AddDays(-(int)now.DayOfWeek), now.Date.AddDays(1))
        };

        var logs = await logRepo.GetByDateRangeAsync(userId, from, to);
        var totals = new Dictionary<int, CategoryTotalDto>();

        foreach (var log in logs)
        {
            foreach (var led in log.LogEntryDimensions)
            {
                if (led.Dimension is null) continue;
                if (!totals.TryGetValue(led.DimensionId, out var dto))
                {
                    dto = new CategoryTotalDto
                    {
                        CategoryId   = led.DimensionId,
                        CategoryName = led.Dimension.Name,
                        ColorHex     = led.Dimension.ColorHex
                    };
                    totals[led.DimensionId] = dto;
                }
                dto.Total += log.PointsRecorded;
            }
        }

        return totals.Values.Where(d => d.Total > 0).OrderByDescending(d => d.Total);
    }

    private static DateTime GetWeekStart(DateTime date) => date.AddDays(-(int)date.DayOfWeek);

    private static Dictionary<int, int> BuildByCategory(
        IEnumerable<Momentum.Domain.Entities.ActivityLog> group, int? categoryId)
    {
        if (categoryId.HasValue) return [];
        return group
            .SelectMany(l => l.LogEntryDimensions
                .Select(led => new { led.DimensionId, l.PointsRecorded }))
            .GroupBy(x => x.DimensionId)
            .ToDictionary(cg => cg.Key, cg => cg.Sum(x => x.PointsRecorded));
    }
}
