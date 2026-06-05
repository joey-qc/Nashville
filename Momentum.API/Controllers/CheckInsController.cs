using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Momentum.Application.Interfaces;
using Momentum.Shared;

namespace Momentum.API.Controllers;

[ApiController]
[Route("api/checkins")]
[Authorize]
public class CheckInsController(ICheckInService checkInService, ILogger<CheckInsController> logger) : ControllerBase
{
    private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    [HttpGet]
    public async Task<IActionResult> GetCheckIns([FromQuery] DateTime? from, [FromQuery] DateTime? to)
    {
        try
        {
            var start = from ?? DateTime.UtcNow.Date;
            var end   = to   ?? DateTime.UtcNow.Date.AddDays(1);
            var items = await checkInService.GetByDateRangeAsync(UserId, start, end);
            return Ok(items);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error fetching check-ins for user {UserId}", UserId);
            return StatusCode(500, "An unexpected error occurred.");
        }
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetCheckIn(int id)
    {
        try
        {
            var item = await checkInService.GetByIdAsync(id, UserId);
            if (item is null) return NotFound();
            return Ok(item);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error fetching check-in {Id} for user {UserId}", id, UserId);
            return StatusCode(500, "An unexpected error occurred.");
        }
    }

    [HttpPost]
    public async Task<IActionResult> CreateCheckIn([FromBody] CreateCheckInRequestDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            var created = await checkInService.CreateAsync(UserId, dto);
            return CreatedAtAction(nameof(GetCheckIn), new { id = created.Id }, created);
        }
        catch (ArgumentException ex)
        {
            logger.LogWarning("Invalid check-in creation attempt by user {UserId}: {Error}", UserId, ex.Message);
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error creating check-in for user {UserId}", UserId);
            return StatusCode(500, "An unexpected error occurred.");
        }
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> UpdateCheckIn(int id, [FromBody] UpdateCheckInRequestDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            var updated = await checkInService.UpdateAsync(id, UserId, dto);
            if (updated is null) return NotFound();
            return Ok(updated);
        }
        catch (ArgumentException ex)
        {
            logger.LogWarning("Invalid check-in update attempt by user {UserId}: {Error}", UserId, ex.Message);
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error updating check-in {Id} for user {UserId}", id, UserId);
            return StatusCode(500, "An unexpected error occurred.");
        }
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteCheckIn(int id)
    {
        try
        {
            var deleted = await checkInService.DeleteAsync(id, UserId);
            if (!deleted) return NotFound();
            return NoContent();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error deleting check-in {Id} for user {UserId}", id, UserId);
            return StatusCode(500, "An unexpected error occurred.");
        }
    }
}
