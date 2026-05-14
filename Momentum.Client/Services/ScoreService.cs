using System.Net.Http.Json;
using Momentum.Shared;

namespace Momentum.Client.Services;

public class ScoreService(HttpClient http)
{
    public async Task<ScoreSummaryDto?> GetSummaryAsync() =>
        await http.GetFromJsonAsync<ScoreSummaryDto>("api/scores/summary");

    public async Task<WeeklyComparisonDto?> GetWeeklyComparisonAsync() =>
        await http.GetFromJsonAsync<WeeklyComparisonDto>("api/scores/weekly-comparison");
}
