using System.Net.Http.Json;
using Momentum.Shared;

namespace Momentum.Client.Services;

public class ScoreService(HttpClient http)
{
    public async Task<ScoreSummaryDto?> GetSummaryAsync()
    {
        var response = await http.GetAsync("api/scores/summary");
        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadFromJsonAsync<ScoreSummaryDto>();
    }

    public async Task<WeeklyComparisonDto?> GetWeeklyComparisonAsync()
    {
        var response = await http.GetAsync("api/scores/weekly-comparison");
        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadFromJsonAsync<WeeklyComparisonDto>();
    }
}
