using System.Net.Http.Json;
using Momentum.Shared;

namespace Momentum.Client.Services;

public class UserSettingsService(HttpClient http)
{
    public async Task<UserSettingsDto?> GetAsync() =>
        await http.GetFromJsonAsync<UserSettingsDto>("api/settings");

    public async Task<UserSettingsDto?> UpdateAsync(UpdateUserSettingsDto dto)
    {
        var response = await http.PutAsJsonAsync("api/settings", dto);
        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadFromJsonAsync<UserSettingsDto>();
    }
}
