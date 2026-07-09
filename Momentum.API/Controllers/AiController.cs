using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Momentum.Application.Interfaces;
using Momentum.Infrastructure.Identity;

namespace Momentum.API.Controllers;

// Read-only AI integration API (AI-001 v1). This is a single-configured-user,
// server-to-server integration authenticated by a shared API key in the
// X-Momentum-AI-Key header — intentionally separate from the per-user JWT pipeline.
[ApiController]
[Route("api/ai")]
[AllowAnonymous]
public class AiController(
    IAiWellnessQueryService aiService,
    UserManager<ApplicationUser> userManager,
    IConfiguration configuration,
    ILogger<AiController> logger) : ControllerBase
{
    private const string ApiKeyHeader = "X-Momentum-AI-Key";

    [HttpGet("today")]
    public async Task<IActionResult> GetToday([FromQuery] int? localOffsetMinutes = null)
    {
        var configuredKey = configuration["Ai:ApiKey"];
        if (string.IsNullOrEmpty(configuredKey))
        {
            logger.LogError("AI API request rejected — Ai:ApiKey is not configured");
            return StatusCode(500, "AI API is not configured.");
        }

        if (!Request.Headers.TryGetValue(ApiKeyHeader, out var providedKey) ||
            !IsValidKey(providedKey.ToString(), configuredKey))
        {
            return Unauthorized();
        }

        var userEmail = configuration["Ai:UserEmail"];
        if (string.IsNullOrEmpty(userEmail))
        {
            logger.LogError("AI API request rejected — Ai:UserEmail is not configured");
            return StatusCode(500, "AI API is not configured.");
        }

        var user = await userManager.FindByEmailAsync(userEmail);
        if (user is null)
        {
            logger.LogError("Configured AI user {Email} was not found", userEmail);
            return StatusCode(500, "AI API is not configured.");
        }

        var offset = localOffsetMinutes ?? configuration.GetValue<int?>("Ai:DefaultLocalOffsetMinutes");

        try
        {
            var result = await aiService.GetTodayAsync(user.Id, offset);
            return Ok(result);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error fetching AI today snapshot for configured AI user");
            return StatusCode(500, "An unexpected error occurred.");
        }
    }

    // Fixed-time comparison so response timing can't be used to brute-force the key byte by byte.
    private static bool IsValidKey(string provided, string configured)
    {
        var providedBytes = Encoding.UTF8.GetBytes(provided);
        var configuredBytes = Encoding.UTF8.GetBytes(configured);
        return providedBytes.Length == configuredBytes.Length &&
               CryptographicOperations.FixedTimeEquals(providedBytes, configuredBytes);
    }
}
