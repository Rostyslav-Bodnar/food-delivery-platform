using DF.Contracts.Gateway.Responses;
using DF.Gateway.API.Attributes;
using DF.Gateway.API.Helpers;
using DF.Gateway.API.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DF.Gateway.API.Controllers.UserService;

[Authorize]
[ApiController]
[Route("api/user")]
[GatewayService(ServiceType.UserService)]
public class UserController(GatewayProxy proxy) : ControllerBase
{
    // =========================
    // GET CURRENT USER
    // =========================
    [HttpGet("profile")]
    public Task<Response<UserDto>> Me()
        => proxy.ProxyAsync<UserDto>(HttpContext);

    // =========================
    // GET USER BY ID
    // =========================
    [HttpGet("user")]
    public Task<Response<UserDto>> GetUser([FromQuery] Guid userId)
        => proxy.ProxyAsync<UserDto>(HttpContext);

    // =========================
    // GET ALL USERS
    // =========================
    [HttpGet("users")]
    public Task<Response<IEnumerable<UserDto>>> GetUsers()
        => proxy.ProxyAsync<IEnumerable<UserDto>>(HttpContext);
}