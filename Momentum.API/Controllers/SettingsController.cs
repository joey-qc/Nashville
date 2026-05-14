using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Momentum.Application.Interfaces;
using Momentum.Shared;

namespace Momentum.API.Controllers;

[ApiController]
[Route("api/settings")]
[Authorize]
public class SettingsController(IUserSettingsService settingsService, ILogger<SettingsController> logger) : ControllerBase
{
    private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    [HttpGet]
    public async Task<IActionResult> GetSettings()
    {
        try
        {
            var settings = await settingsService.GetAsync(UserId);
            if (settings is null) return NotFound();
            return Ok(settings);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error fetching settings for user {UserId}", UserId);
            return StatusCode(500, "An unexpected error occurred.");
        }
    }

    [HttpPut]
    public async Task<IActionResult> UpdateSettings([FromBody] UpdateUserSettingsDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            var updated = await settingsService.UpdateAsync(UserId, dto);
            if (updated is null) return NotFound();
            return Ok(updated);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error updating settings for user {UserId}", UserId);
            return StatusCode(500, "An unexpected error occurred.");
        }
    }
}
