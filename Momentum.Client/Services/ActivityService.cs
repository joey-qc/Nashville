using System.Net.Http.Json;
using Momentum.Shared;

namespace Momentum.Client.Services;

public class ActivityService(HttpClient http)
{
    private List<ActivityDto>? _cache;

    public IReadOnlyList<ActivityDto> Activities => _cache ?? [];

    public async Task LoadAsync()
    {
        _cache = await http.GetFromJsonAsync<List<ActivityDto>>("api/activities") ?? [];
    }

    public void Invalidate() => _cache = null;

    public async Task<IReadOnlyList<ActivityDto>> GetAllAsync()
    {
        if (_cache is null) await LoadAsync();
        return _cache!;
    }

    public async Task<IReadOnlyList<ActivityDto>> GetFrequentAsync(int count = 10)
    {
        var result = await http.GetFromJsonAsync<List<ActivityDto>>($"api/activities/frequent?count={count}");
        return result ?? [];
    }

    public async Task<ActivityDto?> CreateAsync(CreateActivityDto dto)
    {
        var response = await http.PostAsJsonAsync("api/activities", dto);
        if (!response.IsSuccessStatusCode) return null;
        var created = await response.Content.ReadFromJsonAsync<ActivityDto>();
        _cache = null;
        return created;
    }

    public async Task<ActivityDto?> UpdateAsync(int id, UpdateActivityDto dto)
    {
        var response = await http.PutAsJsonAsync($"api/activities/{id}", dto);
        if (!response.IsSuccessStatusCode) return null;
        var updated = await response.Content.ReadFromJsonAsync<ActivityDto>();
        _cache = null;
        return updated;
    }

    public async Task<(bool success, DeleteActivityResponseDto? conflict)> DeleteAsync(int id, string? action = null)
    {
        var url = action is null ? $"api/activities/{id}" : $"api/activities/{id}?action={action}";
        var response = await http.DeleteAsync(url);

        if (response.StatusCode == System.Net.HttpStatusCode.NoContent)
        {
            _cache = null;
            return (true, null);
        }

        if (response.StatusCode == System.Net.HttpStatusCode.Conflict)
        {
            var conflict = await response.Content.ReadFromJsonAsync<DeleteActivityResponseDto>();
            return (false, conflict);
        }

        return (false, null);
    }
}
