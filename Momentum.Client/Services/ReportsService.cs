using System.Net.Http.Json;
using Momentum.Shared;

namespace Momentum.Client.Services;

public class ReportsService(HttpClient http)
{
    public async Task<List<DailyScoreDto>> GetDailyAsync(int days = 30, int? categoryId = null)
    {
        var url = $"api/reports/daily?days={days}";
        if (categoryId.HasValue) url += $"&categoryId={categoryId.Value}";
        return await http.GetFromJsonAsync<List<DailyScoreDto>>(url) ?? [];
    }

    public async Task<List<DailyScoreDto>> GetWeeklyAsync(int weeks = 12, int? categoryId = null)
    {
        var url = $"api/reports/weekly?weeks={weeks}";
        if (categoryId.HasValue) url += $"&categoryId={categoryId.Value}";
        return await http.GetFromJsonAsync<List<DailyScoreDto>>(url) ?? [];
    }

    public async Task<List<DailyScoreDto>> GetMonthlyAsync(int months = 12, int? categoryId = null)
    {
        var url = $"api/reports/monthly?months={months}";
        if (categoryId.HasValue) url += $"&categoryId={categoryId.Value}";
        return await http.GetFromJsonAsync<List<DailyScoreDto>>(url) ?? [];
    }
}
