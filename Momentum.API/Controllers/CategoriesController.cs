using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Momentum.Application.Interfaces;

namespace Momentum.API.Controllers;

[ApiController]
[Route("api/categories")]
[Authorize]
public class CategoriesController(ICategoryService categoryService, ILogger<CategoriesController> logger) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetCategories()
    {
        try
        {
            var categories = await categoryService.GetAllAsync();
            return Ok(categories);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error fetching categories");
            return StatusCode(500, "An unexpected error occurred.");
        }
    }
}
