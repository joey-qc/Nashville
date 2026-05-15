using ApexCharts;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Momentum.Client;
using Momentum.Client.Auth;
using Momentum.Client.Services;
using MudBlazor.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddSingleton<TokenProvider>();
builder.Services.AddScoped<JwtAuthStateProvider>();
builder.Services.AddScoped<AuthenticationStateProvider>(sp =>
    sp.GetRequiredService<JwtAuthStateProvider>());
builder.Services.AddScoped<ClientAuthService>();

builder.Services.AddScoped(sp =>
{
    var tokenProvider = sp.GetRequiredService<TokenProvider>();
    var authStateProvider = sp.GetRequiredService<JwtAuthStateProvider>();
    var handler = new AuthMessageHandler(tokenProvider, authStateProvider)
    {
        InnerHandler = new HttpClientHandler()
    };
    var apiBaseUrl = builder.Configuration["ApiBaseUrl"]!;
    return new HttpClient(handler)
    {
        BaseAddress = new Uri(apiBaseUrl)
    };
});

builder.Services.AddAuthorizationCore();
builder.Services.AddMudServices();
builder.Services.AddApexCharts();

builder.Services.AddScoped<ThemeService>();
builder.Services.AddScoped<CategoryService>();
builder.Services.AddScoped<ActivityService>();
builder.Services.AddScoped<ActivityLogService>();
builder.Services.AddScoped<ScoreService>();
builder.Services.AddScoped<ReportsService>();
builder.Services.AddScoped<UserSettingsService>();

await builder.Build().RunAsync();
