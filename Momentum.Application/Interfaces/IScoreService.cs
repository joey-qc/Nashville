using Momentum.Shared;

namespace Momentum.Application.Interfaces;

public interface IScoreService
{
    Task<ScoreSummaryDto> GetSummaryAsync(string userId);
    Task<WeeklyComparisonDto> GetWeeklyComparisonAsync(string userId);
    Task<IEnumerable<DailyScoreDto>> GetDailyTotalsAsync(string userId, int days, int? categoryId);
    Task<IEnumerable<DailyScoreDto>> GetWeeklyTotalsAsync(string userId, int weeks, int? categoryId);
    Task<IEnumerable<DailyScoreDto>> GetMonthlyTotalsAsync(string userId, int months, int? categoryId);
    Task<IEnumerable<CategoryTotalDto>> GetCategoryTotalsAsync(string userId, string period);
}
