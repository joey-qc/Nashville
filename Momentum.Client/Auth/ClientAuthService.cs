using System.Net.Http.Json;
using Momentum.Client.Services;
using Momentum.Shared;

namespace Momentum.Client.Auth;

public class ClientAuthService(
    HttpClient http,
    JwtAuthStateProvider authStateProvider,
    ActivityService activityService,
    CategoryService categoryService,
    UserSettingsService settingsService,
    ThemeService themeService)
{
    public async Task<AuthResponse> LoginAsync(LoginRequest request)
    {
        var response = await http.PostAsJsonAsync("api/auth/login", request);
        var result = await response.Content.ReadFromJsonAsync<AuthResponse>() ?? new AuthResponse();

        if (result.Succeeded && result.Token is not null)
        {
            await authStateProvider.MarkUserAsAuthenticated(result.Token);
            await Task.WhenAll(
                categoryService.LoadAsync(),
                activityService.LoadAsync(),
                ApplyThemeAsync());
        }

        return result;
    }

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
    {
        var response = await http.PostAsJsonAsync("api/auth/register", request);
        var result = await response.Content.ReadFromJsonAsync<AuthResponse>() ?? new AuthResponse();

        if (result.Succeeded && result.Token is not null)
        {
            await authStateProvider.MarkUserAsAuthenticated(result.Token);
            await Task.WhenAll(
                categoryService.LoadAsync(),
                activityService.LoadAsync(),
                ApplyThemeAsync());
        }

        return result;
    }

    public async Task LogoutAsync()
    {
        categoryService.Invalidate();
        activityService.Invalidate();
        themeService.Apply("light");
        await authStateProvider.MarkUserAsLoggedOut();
    }

    private async Task ApplyThemeAsync()
    {
        var settings = await settingsService.GetAsync();
        if (settings is not null)
            themeService.Apply(settings.Theme);
    }
}
