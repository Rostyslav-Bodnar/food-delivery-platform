using Microsoft.AspNetCore.Mvc;
using DF.UserService.Application.Interfaces;
using DF.UserService.Contracts.Models.Request;

[ApiController]
[Route("api/[controller]")]
public class AuthController(IAuthService authService) : ControllerBase
{
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        var tokens = await authService.RegisterAsync(request);
        return Ok(tokens);
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var tokens = await authService.LoginAsync(request);
        return Ok(tokens);
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh([FromBody] RefreshRequest request)
    {
        var tokens = await authService.RefreshAsync(request.RefreshToken);
        if (tokens == null) return Unauthorized("Invalid or expired refresh token");
        return Ok(tokens);
    }

    [HttpPost("revoke")]
    public async Task<IActionResult> Revoke([FromBody] RefreshRequest request)
    {
        await authService.RevokeAsync(request.RefreshToken);
        return NoContent();
    }
}
