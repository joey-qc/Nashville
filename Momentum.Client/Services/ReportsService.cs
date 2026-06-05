using System.Net.Http.Json;
using Momentum.Shared;

namespace Momentum.Client.Services;

public class ReportsService(HttpClient http)
{
    public async Task<List<DailyScoreDto>> GetDailyAsync(int days = 30, int? categoryId = null)
    {
        var offset = (int)TimeZoneInfo.Local.GetUtcOffset(DateTime.Now).TotalMinutes;
        var url = $"api/reports/daily?days={days}&localOffsetMinutes={offset}";
        if (categoryId.HasValue) url += $"&categoryId={categoryId.Value}";
        var response = await http.GetAsync(url);
        if (!response.IsSuccessStatusCode) return [];
        return await response.Content.ReadFromJsonAsync<List<DailyScoreDto>>() ?? [];
    }

    public async Task<List<DailyScoreDto>> GetWeeklyAsync(int weeks = 12, int? categoryId = null)
    {
        var url = $"api/reports/weekly?weeks={weeks}";
        if (categoryId.HasValue) url += $"&categoryId={categoryId.Value}";
        var response = await http.GetAsync(url);
        if (!response.IsSuccessStatusCode) return [];
        return await response.Content.ReadFromJsonAsync<List<DailyScoreDto>>() ?? [];
    }

    public async Task<List<DailyScoreDto>> GetMonthlyAsync(int months = 12, int? categoryId = null)
    {
        var url = $"api/reports/monthly?months={months}";
        if (categoryId.HasValue) url += $"&categoryId={categoryId.Value}";
        var response = await http.GetAsync(url);
        if (!response.IsSuccessStatusCode) return [];
        return await response.Content.ReadFromJsonAsync<List<DailyScoreDto>>() ?? [];
    }

    public async Task<List<CategoryTotalDto>> GetBalanceAsync(string period = "week")
    {
        var response = await http.GetAsync($"api/reports/balance?period={period}");
        if (!response.IsSuccessStatusCode) return [];
        return await response.Content.ReadFromJsonAsync<List<CategoryTotalDto>>() ?? [];
    }
}
