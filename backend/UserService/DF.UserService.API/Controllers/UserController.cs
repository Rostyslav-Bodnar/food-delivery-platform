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
    
    [Authorize]
    [HttpGet("profile")]
    public async Task<IActionResult> Me()
    {
        var userId = Guid.Parse(User.FindFirst("sub")!.Value);
        var result = await userService.GetUserAsync(userId);
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