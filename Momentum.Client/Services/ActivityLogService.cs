using System.Net.Http.Json;
using Momentum.Shared;

namespace Momentum.Client.Services;

public class ActivityLogService(HttpClient http)
{
    // Explicitly tag LoggedAt as UTC after deserialization so that any call to
    // ToLocalTime() in Razor components always has correct kind information.
    // The API guarantees Z-suffixed datetimes; System.Text.Json deserializes those
    // as DateTimeKind.Utc, but SpecifyKind here acts as a defensive guarantee.
    private static ActivityLogDto? TagUtc(ActivityLogDto? log)
    {
        if (log is not null)
            log.LoggedAt = DateTime.SpecifyKind(log.LoggedAt, DateTimeKind.Utc);
        return log;
    }

    private static List<ActivityLogDto> TagUtc(List<ActivityLogDto> logs)
    {
        foreach (var log in logs)
            log.LoggedAt = DateTime.SpecifyKind(log.LoggedAt, DateTimeKind.Utc);
        return logs;
    }

    public async Task<List<ActivityLogDto>> GetByDateRangeAsync(DateTime from, DateTime to)
    {
        var response = await http.GetAsync($"api/logs?from={from:O}&to={to:O}");
        if (!response.IsSuccessStatusCode) return [];
        var result = await response.Content.ReadFromJsonAsync<List<ActivityLogDto>>() ?? [];
        return TagUtc(result);
    }

    public async Task<ActivityLogDto?> GetByIdAsync(int id)
    {
        try
        {
            var response = await http.GetAsync($"api/logs/{id}");
            if (!response.IsSuccessStatusCode) return null;
            return TagUtc(await response.Content.ReadFromJsonAsync<ActivityLogDto>());
        }
        catch
        {
            return null;
        }
    }

    public async Task<ActivityLogDto?> CreateAsync(CreateActivityLogDto dto)
    {
        var response = await http.PostAsJsonAsync("api/logs", dto);
        if (!response.IsSuccessStatusCode) return null;
        return TagUtc(await response.Content.ReadFromJsonAsync<ActivityLogDto>());
    }

    public async Task<ActivityLogDto?> UpdateAsync(int id, UpdateActivityLogDto dto)
    {
        var response = await http.PutAsJsonAsync($"api/logs/{id}", dto);
        if (!response.IsSuccessStatusCode) return null;
        return TagUtc(await response.Content.ReadFromJsonAsync<ActivityLogDto>());
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var response = await http.DeleteAsync($"api/logs/{id}");
        return response.IsSuccessStatusCode;
    }
}
