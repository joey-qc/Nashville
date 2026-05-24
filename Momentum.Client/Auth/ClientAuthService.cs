using System.Net.Http.Json;
using Momentum.Client.Services;
using Momentum.Shared;

namespace Momentum.Client.Auth;

public class ClientAuthService(
    HttpClient http,
    JwtAuthStateProvider authStateProvider,
    ActivityService activityService,
    CategoryService categoryService)
{
    public async Task<AuthResponse> LoginAsync(LoginRequest request)
    {
        try
        {
            var response = await http.PostAsJsonAsync("api/auth/login", request);
            var result = await response.Content.ReadFromJsonAsync<AuthResponse>() ?? new AuthResponse();

            if (result.Succeeded && result.Token is not null)
            {
                await authStateProvider.MarkUserAsAuthenticated(result.Token);
                await Task.WhenAll(
                    categoryService.LoadAsync(),
                    activityService.LoadAsync());
            }

            return result;
        }
        catch (HttpRequestException ex)
        {
            return new AuthResponse
            {
                Succeeded = false,
                Errors = [ex.Message]
            };
        }
    }

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
    {
        try
        {
            var response = await http.PostAsJsonAsync("api/auth/register", request);
            var result = await response.Content.ReadFromJsonAsync<AuthResponse>() ?? new AuthResponse();

            if (result.Succeeded && result.Token is not null)
            {
                await authStateProvider.MarkUserAsAuthenticated(result.Token);
                await Task.WhenAll(
                    categoryService.LoadAsync(),
                    activityService.LoadAsync());
            }

            return result;
        }
        catch (HttpRequestException ex)
        {
            return new AuthResponse
            {
                Succeeded = false,
                Errors = [ex.Message]
            };
        }
    }

    public async Task LogoutAsync()
    {
        categoryService.Invalidate();
        activityService.Invalidate();
        await authStateProvider.MarkUserAsLoggedOut();
    }
}
