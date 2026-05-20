using DF.Contracts.Gateway.Responses;
using DF.UserService.API.Middlewares;
using DF.UserService.Application.Services.Interfaces;
using DF.UserService.Contracts.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace DF.UserService.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UserController(IUserService userService, IUserContext userContext) : ControllerBase
{
    [HttpGet("profile")]
    public async Task<ActionResult<UserDto>> Me()
    {
        var userId = userContext.UserId;
        if(userId == null)
            throw new UnauthorizedAccessException("User is not authenticated");

        var user = await userService.GetUserAsync(userId);

        return Ok(user);
    }

    [HttpGet("user")]
    public async Task<ActionResult<UserDto>> GetUser([FromQuery] Guid userId)
    {
        var user = await userService.GetUserAsync(userId);
        return Ok(user);
    }

    [HttpGet("users")]
    public async Task<ActionResult<IEnumerable<UserDto>>> GetUsers()
    {
        var users = await userService.GetAllUsers();

        if (!users.Any())
            throw new NotFoundException("No users found");

        return Ok(users);
    }
}