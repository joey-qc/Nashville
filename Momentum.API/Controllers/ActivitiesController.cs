using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Momentum.Application.Interfaces;
using Momentum.Shared;

namespace Momentum.API.Controllers;

[ApiController]
[Route("api/activities")]
[Authorize]
public class ActivitiesController(IActivityService activityService, ILogger<ActivitiesController> logger) : ControllerBase
{
    private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    [HttpGet]
    public async Task<IActionResult> GetActivities()
    {
        try
        {
            var activities = await activityService.GetAllAsync(UserId);
            return Ok(activities);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error fetching activities for user {UserId}", UserId);
            return StatusCode(500, "An unexpected error occurred.");
        }
    }

    [HttpGet("frequent")]
    public async Task<IActionResult> GetFrequent([FromQuery] int count = 10)
    {
        try
        {
            var activities = await activityService.GetFrequentAsync(UserId, count);
            return Ok(activities);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error fetching frequent activities for user {UserId}", UserId);
            return StatusCode(500, "An unexpected error occurred.");
        }
    }

    [HttpPost]
    public async Task<IActionResult> CreateActivity([FromBody] CreateActivityDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            var created = await activityService.CreateAsync(UserId, dto);
            return CreatedAtAction(nameof(GetActivities), new { id = created.Id }, created);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error creating activity for user {UserId}", UserId);
            return StatusCode(500, "An unexpected error occurred.");
        }
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> UpdateActivity(int id, [FromBody] UpdateActivityDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            var updated = await activityService.UpdateAsync(id, UserId, dto);
            if (updated is null) return NotFound();
            return Ok(updated);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error updating activity {Id} for user {UserId}", id, UserId);
            return StatusCode(500, "An unexpected error occurred.");
        }
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteActivity(int id, [FromQuery] string? action)
    {
        try
        {
            var (deleted, logCount) = await activityService.DeleteAsync(id, UserId, action);

            if (!deleted && logCount == 0) return NotFound();

            if (!deleted && logCount > 0)
                return Conflict(new DeleteActivityResponseDto { LogCount = logCount });

            return NoContent();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error deleting activity {Id} for user {UserId}", id, UserId);
            return StatusCode(500, "An unexpected error occurred.");
        }
    }
}
