using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Momentum.Application.Interfaces;

namespace Momentum.API.Controllers;

[ApiController]
[Route("api/scores")]
[Authorize]
public class ScoresController(IScoreService scoreService, ILogger<ScoresController> logger) : ControllerBase
{
    private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    [HttpGet("summary")]
    public async Task<IActionResult> GetSummary(
        [FromQuery] DateTime? todayStartUtc = null,
        [FromQuery] DateTime? weekStartUtc  = null,
        [FromQuery] DateTime? monthStartUtc = null)
    {
        try
        {
            var summary = await scoreService.GetSummaryAsync(UserId, todayStartUtc, weekStartUtc, monthStartUtc);
            return Ok(summary);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error fetching score summary for user {UserId}", UserId);
            return StatusCode(500, "An unexpected error occurred.");
        }
    }

    [HttpGet("weekly-comparison")]
    public async Task<IActionResult> GetWeeklyComparison()
    {
        try
        {
            var comparison = await scoreService.GetWeeklyComparisonAsync(UserId);
            return Ok(comparison);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error fetching weekly comparison for user {UserId}", UserId);
            return StatusCode(500, "An unexpected error occurred.");
        }
    }
}
