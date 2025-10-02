using DF.UserService.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DF.UserService.API.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class UserController : ControllerBase
{
    private readonly IUserService userService;
    
    public UserController(IUserService userService)
    {
        this.userService = userService;
    }
    
    [HttpGet("profile")]
    [Authorize]
    public async Task<IActionResult> Me()
    {
        // Отримуємо клейм NameIdentifier
        var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);

        if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
            return Unauthorized("Invalid token or user id");

        var result = await userService.GetUserAsync(userId);
        if (result == null)
            return NotFound("User not found");

        return Ok(result);
    }


    [HttpGet("user")]
    public async Task<IActionResult> GetUser([FromQuery] Guid userId)
    {
        var user = await userService.GetUserAsync(userId);
        return Ok(user);
    }

    [HttpGet("users")]
    public async Task<IActionResult> GetUsers()
    {
        var users = await userService.GetAllUsers();
        return Ok(users);
    }
}