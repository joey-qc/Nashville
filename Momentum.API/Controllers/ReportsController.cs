using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Momentum.Application.Interfaces;

namespace Momentum.API.Controllers;

[ApiController]
[Route("api/reports")]
[Authorize]
public class ReportsController(IScoreService scoreService, ILogger<ReportsController> logger) : ControllerBase
{
    private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    [HttpGet("daily")]
    public async Task<IActionResult> GetDaily([FromQuery] int days = 30, [FromQuery] int? categoryId = null)
    {
        try
        {
            var data = await scoreService.GetDailyTotalsAsync(UserId, days, categoryId);
            return Ok(data);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error fetching daily report for user {UserId}", UserId);
            return StatusCode(500, "An unexpected error occurred.");
        }
    }

    [HttpGet("weekly")]
    public async Task<IActionResult> GetWeekly([FromQuery] int weeks = 12, [FromQuery] int? categoryId = null)
    {
        try
        {
            var data = await scoreService.GetWeeklyTotalsAsync(UserId, weeks, categoryId);
            return Ok(data);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error fetching weekly report for user {UserId}", UserId);
            return StatusCode(500, "An unexpected error occurred.");
        }
    }

    [HttpGet("monthly")]
    public async Task<IActionResult> GetMonthly([FromQuery] int months = 12, [FromQuery] int? categoryId = null)
    {
        try
        {
            var data = await scoreService.GetMonthlyTotalsAsync(UserId, months, categoryId);
            return Ok(data);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error fetching monthly report for user {UserId}", UserId);
            return StatusCode(500, "An unexpected error occurred.");
        }
    }

    [HttpGet("balance")]
    public async Task<IActionResult> GetBalance([FromQuery] string period = "week")
    {
        try
        {
            var data = await scoreService.GetCategoryTotalsAsync(UserId, period);
            return Ok(data);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error fetching balance report for user {UserId}", UserId);
            return StatusCode(500, "An unexpected error occurred.");
        }
    }
}
