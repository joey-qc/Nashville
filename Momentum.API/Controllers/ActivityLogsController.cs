using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Momentum.Application.Interfaces;
using Momentum.Shared;

namespace Momentum.API.Controllers;

[ApiController]
[Route("api/logs")]
[Authorize]
public class ActivityLogsController(IActivityLogService logService, ILogger<ActivityLogsController> logger) : ControllerBase
{
    private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetLog(int id)
    {
        try
        {
            var log = await logService.GetByIdAsync(id, UserId);
            if (log is null) return NotFound();
            return Ok(log);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error fetching log {Id} for user {UserId}", id, UserId);
            return StatusCode(500, "An unexpected error occurred.");
        }
    }

    [HttpGet]
    public async Task<IActionResult> GetLogs([FromQuery] DateTime? from, [FromQuery] DateTime? to)
    {
        try
        {
            var start = from ?? DateTime.UtcNow.Date;
            var end = to ?? DateTime.UtcNow.Date.AddDays(1);
            var logs = await logService.GetByDateRangeAsync(UserId, start, end);
            return Ok(logs);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error fetching logs for user {UserId}", UserId);
            return StatusCode(500, "An unexpected error occurred.");
        }
    }

    [HttpPost]
    public async Task<IActionResult> CreateLog([FromBody] CreateActivityLogDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            var created = await logService.CreateAsync(UserId, dto);
            return CreatedAtAction(nameof(GetLogs), new { id = created.Id }, created);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error creating log for user {UserId}", UserId);
            return StatusCode(500, "An unexpected error occurred.");
        }
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> UpdateLog(int id, [FromBody] UpdateActivityLogDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            var updated = await logService.UpdateAsync(id, UserId, dto);
            if (updated is null) return NotFound();
            return Ok(updated);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error updating log {Id} for user {UserId}", id, UserId);
            return StatusCode(500, "An unexpected error occurred.");
        }
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteLog(int id)
    {
        try
        {
            var deleted = await logService.DeleteAsync(id, UserId);
            if (!deleted) return NotFound();
            return NoContent();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error deleting log {Id} for user {UserId}", id, UserId);
            return StatusCode(500, "An unexpected error occurred.");
        }
    }
}
