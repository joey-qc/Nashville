using Microsoft.AspNetCore.Mvc;
using Momentum.Application.Interfaces;
using Momentum.Shared;

namespace Momentum.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController(IAuthService authService) : ControllerBase
{
    [HttpPost("register")]
    public async Task<ActionResult<AuthResponse>> Register(RegisterRequest request)
    {
        var result = await authService.RegisterAsync(request);
        return result.Succeeded ? Ok(result) : BadRequest(result);
    }

    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login(LoginRequest request)
    {
        var result = await authService.LoginAsync(request);
        return result.Succeeded ? Ok(result) : Unauthorized(result);
    }
}
