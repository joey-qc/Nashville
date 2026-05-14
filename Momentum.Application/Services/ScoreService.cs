using Momentum.Application.Interfaces;
using Momentum.Shared;

namespace Momentum.Application.Services;

public class ScoreService(IActivityLogRepository logRepo) : IScoreService
{
    public async Task<ScoreSummaryDto> GetSummaryAsync(string userId)
    {
        var now = DateTime.UtcNow;
        var todayStart = now.Date;
        var weekStart = todayStart.AddDays(-(int)now.DayOfWeek);
        var monthStart = new DateTime(now.Year, now.Month, 1);

        var monthLogs = await logRepo.GetByDateRangeAsync(userId, monthStart, now.AddDays(1));
        var list = monthLogs.ToList();

        return new ScoreSummaryDto
        {
            TodayTotal = list.Where(l => l.LoggedAt >= todayStart).Sum(l => l.PointsRecorded),
            WeekTotal = list.Where(l => l.LoggedAt >= weekStart).Sum(l => l.PointsRecorded),
            MonthTotal = list.Sum(l => l.PointsRecorded)
        };
    }

    public async Task<WeeklyComparisonDto> GetWeeklyComparisonAsync(string userId)
    {
        var today = DateTime.UtcNow.Date;
        var thisWeekStart = today.AddDays(-(int)today.DayOfWeek);
        var lastWeekStart = thisWeekStart.AddDays(-7);

        var twoWeeksLogs = await logRepo.GetByDateRangeAsync(userId, lastWeekStart, thisWeekStart.AddDays(7));
        var list = twoWeeksLogs.ToList();

        var days = new[] { "Sun", "Mon", "Tue", "Wed", "Thu", "Fri", "Sat" };

        return new WeeklyComparisonDto
        {
            Days = days.Select((label, i) => new DayComparisonDto
            {
                DayLabel = label,
                CurrentWeek = list
                    .Where(l => l.LoggedAt.Date == thisWeekStart.AddDays(i))
                    .Sum(l => l.PointsRecorded),
                LastWeek = list
                    .Where(l => l.LoggedAt.Date == lastWeekStart.AddDays(i))
                    .Sum(l => l.PointsRecorded)
            }).ToList()
        };
    }

    public async Task<IEnumerable<DailyScoreDto>> GetDailyTotalsAsync(string userId, int days, int? categoryId)
    {
        var to = DateTime.UtcNow.Date.AddDays(1);
        var from = to.AddDays(-days);
        return await GetTotalsAsync(userId, from, to, "day", categoryId);
    }

    public async Task<IEnumerable<DailyScoreDto>> GetWeeklyTotalsAsync(string userId, int weeks, int? categoryId)
    {
        var to = DateTime.UtcNow.Date.AddDays(1);
        var from = to.AddDays(-weeks * 7);
        return await GetTotalsAsync(userId, from, to, "week", categoryId);
    }

    public async Task<IEnumerable<DailyScoreDto>> GetMonthlyTotalsAsync(string userId, int months, int? categoryId)
    {
        var to = DateTime.UtcNow.Date.AddDays(1);
        var from = to.AddMonths(-months);
        return await GetTotalsAsync(userId, from, to, "month", categoryId);
    }

    private async Task<IEnumerable<DailyScoreDto>> GetTotalsAsync(
        string userId, DateTime from, DateTime to, string groupBy, int? categoryId)
    {
        var logs = await logRepo.GetByDateRangeAsync(userId, from, to);

        var filtered = categoryId.HasValue
            ? logs.Where(l => l.Activity?.Categories.Any(ac => ac.CategoryId == categoryId.Value) == true)
            : logs;

        return groupBy switch
        {
            "week" => filtered
                .GroupBy(l => GetWeekStart(l.LoggedAt.Date))
                .OrderBy(g => g.Key)
                .Select(g => new DailyScoreDto
                {
                    Date = DateOnly.FromDateTime(g.Key),
                    Total = g.Sum(l => l.PointsRecorded)
                }),
            "month" => filtered
                .GroupBy(l => new DateTime(l.LoggedAt.Year, l.LoggedAt.Month, 1))
                .OrderBy(g => g.Key)
                .Select(g => new DailyScoreDto
                {
                    Date = DateOnly.FromDateTime(g.Key),
                    Total = g.Sum(l => l.PointsRecorded)
                }),
            _ => filtered
                .GroupBy(l => l.LoggedAt.Date)
                .OrderBy(g => g.Key)
                .Select(g => new DailyScoreDto
                {
                    Date = DateOnly.FromDateTime(g.Key),
                    Total = g.Sum(l => l.PointsRecorded)
                })
        };
    }

    private static DateTime GetWeekStart(DateTime date) => date.AddDays(-(int)date.DayOfWeek);
}
