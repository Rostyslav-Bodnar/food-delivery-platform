using DF.Contracts.Gateway.Responses;
using DF.UserService.API.Middlewares;
using DF.UserService.Application.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace DF.UserService.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UserController(IUserService userService, IUserContext userContext) : ControllerBase
{
    // =========================
    // GET CURRENT USER PROFILE
    // =========================
    [HttpGet("profile")]
    public async Task<ActionResult<UserDto>> Me()
    {
        var userId = userContext.UserId;

        if (userId == null)
        {
            return Unauthorized(new ServiceErrorResponse(
                Code: "UNAUTHORIZED",
                Message: "User is not authenticated"
            ));
        }

        var user = await userService.GetUserAsync(userId);
        if (user == null)
        {
            return NotFound(new ServiceErrorResponse(
                Code: "USER_NOT_FOUND",
                Message: "User not found"
            ));
        }

        return Ok(user);
    }

    // =========================
    // GET USER BY ID
    // =========================
    [HttpGet("user")]
    public async Task<ActionResult<UserDto>> GetUser([FromQuery] Guid userId)
    {
        var user = await userService.GetUserAsync(userId);

        if (user == null)
        {
            return NotFound(new ServiceErrorResponse(
                Code: "USER_NOT_FOUND",
                Message: "User not found"
            ));
        }

        return Ok(user);
    }

    // =========================
    // GET ALL USERS
    // =========================
    [HttpGet("users")]
    public async Task<ActionResult<IEnumerable<UserDto>>> GetUsers()
    {
        var users = await userService.GetAllUsers();

        if (users == null || !users.Any())
        {
            return NotFound(new ServiceErrorResponse(
                Code: "USERS_NOT_FOUND",
                Message: "No users found"
            ));
        }

        return Ok(users);
    }
}