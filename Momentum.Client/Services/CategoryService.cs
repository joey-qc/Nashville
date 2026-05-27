using System.Net.Http.Json;
using Momentum.Shared;

namespace Momentum.Client.Services;

public class CategoryService(HttpClient http)
{
    private List<CategoryDto>? _cache;

    public IReadOnlyList<CategoryDto> Categories => _cache ?? [];

    public async Task LoadAsync()
    {
        // Use GetAsync so we can inspect the status code before reading the body.
        // GetFromJsonAsync calls EnsureSuccessStatusCode internally and throws on 401/403,
        // which would surface as an unhandled exception during logout race conditions.
        var response = await http.GetAsync("api/categories");
        if (!response.IsSuccessStatusCode)
        {
            // 401 = token cleared during logout; 403 = revoked; treat as empty cache.
            _cache = [];
            return;
        }
        _cache = await response.Content.ReadFromJsonAsync<List<CategoryDto>>() ?? [];
    }

    public async Task<IReadOnlyList<CategoryDto>> GetAllAsync()
    {
        if (_cache is null) await LoadAsync();
        return _cache!;
    }

    public void Invalidate() => _cache = null;

    public string GetColor(int id) =>
        _cache?.FirstOrDefault(c => c.Id == id)?.ColorHex ?? "#9E9E9E";
}
