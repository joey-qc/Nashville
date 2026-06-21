using Momentum.Shared;

namespace Momentum.Application.Interfaces;

public interface IScoreService
{
    Task<ScoreSummaryDto> GetSummaryAsync(string userId,
        DateTime? todayStartUtc = null, DateTime? weekStartUtc = null, DateTime? monthStartUtc = null);
    Task<WeeklyComparisonDto> GetWeeklyComparisonAsync(string userId, int? localOffsetMinutes = null);
    Task<IEnumerable<DailyScoreDto>> GetDailyTotalsAsync(string userId, int days, int? categoryId, int? localOffsetMinutes = null, DateOnly? anchorDate = null);
    Task<IEnumerable<DailyScoreDto>> GetWeeklyTotalsAsync(string userId, int weeks, int? categoryId, DateOnly? anchorDate = null);
    Task<IEnumerable<DailyScoreDto>> GetMonthlyTotalsAsync(string userId, int months, int? categoryId, DateOnly? anchorDate = null);
    Task<IEnumerable<CategoryTotalDto>> GetCategoryTotalsAsync(string userId, string period, DateOnly? anchorDate = null);
}
