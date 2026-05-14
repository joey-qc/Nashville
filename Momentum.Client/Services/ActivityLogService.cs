using System.Net.Http.Json;
using Momentum.Shared;

namespace Momentum.Client.Services;

public class ActivityLogService(HttpClient http)
{
    public async Task<List<ActivityLogDto>> GetByDateRangeAsync(DateTime from, DateTime to)
    {
        var result = await http.GetFromJsonAsync<List<ActivityLogDto>>(
            $"api/logs?from={from:O}&to={to:O}");
        return result ?? [];
    }

    public async Task<ActivityLogDto?> GetByIdAsync(int id)
    {
        try
        {
            return await http.GetFromJsonAsync<ActivityLogDto>($"api/logs/{id}");
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
        return await response.Content.ReadFromJsonAsync<ActivityLogDto>();
    }

    public async Task<ActivityLogDto?> UpdateAsync(int id, UpdateActivityLogDto dto)
    {
        var response = await http.PutAsJsonAsync($"api/logs/{id}", dto);
        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadFromJsonAsync<ActivityLogDto>();
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var response = await http.DeleteAsync($"api/logs/{id}");
        return response.IsSuccessStatusCode;
    }
}
