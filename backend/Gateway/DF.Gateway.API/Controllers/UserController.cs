using System.Security.Claims;
using DF.Contracts.Gateway.Responses;
using DF.Gateway.API.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DF.Gateway.API.Controllers;

[Authorize]
[ApiController]
[Route("api/user")]
public class UserController(InternalHttpClient http) : ControllerBase
{
    // =========================
    // GET CURRENT USER
    // =========================
    [HttpGet("profile")]
    public async Task<ActionResult<Response<UserDto>>> Me()
        => await Forward<UserDto>("/api/user/profile");

    // =========================
    // GET USER BY ID
    // =========================
    [HttpGet("user")]
    public async Task<ActionResult<Response<UserDto>>> GetUser(
        [FromQuery] Guid userId)
        => await Forward<UserDto>($"/api/user/user?userId={userId}");

    // =========================
    // GET ALL USERS
    // =========================
    [HttpGet("users")]
    public async Task<ActionResult<Response<IEnumerable<UserDto>>>> GetUsers()
        => await Forward<IEnumerable<UserDto>>("/api/user/users");

    // =========================
    // INTERNAL
    // =========================
    private async Task<ActionResult<Response<T>>> Forward<T>(string url)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Ok(new Response<T>(false, default, "User not authenticated"));
        }

        var request = new HttpRequestMessage(HttpMethod.Get, url);

        // 👇 ОЦЕ ЗАРАЗ ГОЛОВНЕ
        var response = await http.SendAsync(request, userId);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content
                .ReadFromJsonAsync<ServiceErrorResponse>();

            return Ok(new Response<T>(
                false,
                default,
                error?.Message ?? "User service error"
            ));
        }

        var data = await response.Content.ReadFromJsonAsync<T>();
        return Ok(new Response<T>(true, data!));
    }
}