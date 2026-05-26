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
            _ => filtered
                .GroupBy(l => l.LoggedAt.Date)
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
            if (log.Activity?.Categories is null) continue;
            foreach (var ac in log.Activity.Categories)
            {
                if (ac.Category is null) continue;
                if (!totals.TryGetValue(ac.CategoryId, out var dto))
                {
                    dto = new CategoryTotalDto
                    {
                        CategoryId = ac.CategoryId,
                        CategoryName = ac.Category.Name,
                        ColorHex = ac.Category.ColorHex
                    };
                    totals[ac.CategoryId] = dto;
                }
                dto.Total += log.PointsRecorded;
            }
        }

        return totals.Values.Where(d => d.Total > 0).OrderByDescending(d => d.Total);
    }

    private static DateTime GetWeekStart(DateTime date) => date.AddDays(-(int)date.DayOfWeek);

    // When no category filter is applied, compute points per category for each period bucket
    // so the client can render stacked bars. When a category is filtered, ByCategory is empty.
    private static Dictionary<int, int> BuildByCategory(
        IEnumerable<Momentum.Domain.Entities.ActivityLog> group, int? categoryId)
    {
        if (categoryId.HasValue) return [];
        return group
            .SelectMany(l => (l.Activity?.Categories
                              ?? Enumerable.Empty<Momentum.Domain.Entities.ActivityCategory>())
                .Select(ac => new { ac.CategoryId, l.PointsRecorded }))
            .GroupBy(x => x.CategoryId)
            .ToDictionary(cg => cg.Key, cg => cg.Sum(x => x.PointsRecorded));
    }
}
