using System.Net.Http.Json;
using Momentum.Shared;

namespace Momentum.Client.Services;

public class CategoryService(HttpClient http)
{
    private List<CategoryDto>? _cache;

    public IReadOnlyList<CategoryDto> Categories => _cache ?? [];

    public async Task LoadAsync()
    {
        _cache = await http.GetFromJsonAsync<List<CategoryDto>>("api/categories") ?? [];
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
