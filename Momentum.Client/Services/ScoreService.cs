using System.Net.Http.Json;
using Momentum.Shared;

namespace Momentum.Client.Services;

public class ScoreService(HttpClient http)
{
    public async Task<ScoreSummaryDto?> GetSummaryAsync()
    {
        // Compute UTC equivalents of local-day boundaries in the browser so the server
        // can filter by the user's local calendar day rather than UTC midnight.
        var localToday    = DateTime.Today; // browser local midnight (DateTimeKind.Local)
        var todayStart    = localToday.ToUniversalTime();
        var weekStart     = localToday.AddDays(-(int)localToday.DayOfWeek).ToUniversalTime();
        var monthStart    = localToday.AddDays(1 - localToday.Day).ToUniversalTime();

        var url = $"api/scores/summary" +
                  $"?todayStartUtc={todayStart:O}" +
                  $"&weekStartUtc={weekStart:O}" +
                  $"&monthStartUtc={monthStart:O}";

        var response = await http.GetAsync(url);
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
