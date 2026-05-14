using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.JSInterop;

namespace Momentum.Client.Auth;

public class JwtAuthStateProvider(IJSRuntime js, TokenProvider tokenProvider) : AuthenticationStateProvider
{
    private const string TokenKey = "authToken";
    private static readonly AuthenticationState Anonymous = new(new ClaimsPrincipal(new ClaimsIdentity()));

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        var token = await js.InvokeAsync<string?>("localStorage.getItem", TokenKey);
        if (string.IsNullOrWhiteSpace(token))
            return Anonymous;

        var principal = BuildPrincipal(token);
        if (principal is null)
            return Anonymous;

        tokenProvider.Token = token;
        return new AuthenticationState(principal);
    }

    public async Task MarkUserAsAuthenticated(string token)
    {
        await js.InvokeVoidAsync("localStorage.setItem", TokenKey, token);
        tokenProvider.Token = token;
        var principal = BuildPrincipal(token);
        NotifyAuthenticationStateChanged(Task.FromResult(
            principal is null ? Anonymous : new AuthenticationState(principal)));
    }

    public async Task MarkUserAsLoggedOut()
    {
        await js.InvokeVoidAsync("localStorage.removeItem", TokenKey);
        tokenProvider.Token = null;
        NotifyAuthenticationStateChanged(Task.FromResult(Anonymous));
    }

    private static ClaimsPrincipal? BuildPrincipal(string token)
    {
        try
        {
            var handler = new JwtSecurityTokenHandler();
            var jwt = handler.ReadJwtToken(token);
            if (jwt.ValidTo < DateTime.UtcNow)
                return null;

            var identity = new ClaimsIdentity(jwt.Claims, "jwt");
            return new ClaimsPrincipal(identity);
        }
        catch { return null; }
    }
}
