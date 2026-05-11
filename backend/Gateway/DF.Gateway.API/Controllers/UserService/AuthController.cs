using DF.Contracts.Gateway.Requests.Auth;
using DF.Contracts.Gateway.Responses;
using DF.Gateway.API.Attributes;
using DF.Gateway.API.Helpers;
using DF.Gateway.API.Infrastructure;
using Microsoft.AspNetCore.Mvc;

namespace DF.Gateway.API.Controllers.UserService;

[ApiController]
[Route("api/[controller]")]
[GatewayServiceAttribute(ServiceType.UserService)]
public class AuthController(GatewayProxy proxy) : ControllerBase
{
    
    [HttpPost("register")]
    public Task<Response<TokenResponse>> Register()
        => proxy.ProxyAsync<TokenResponse>(HttpContext);

    [HttpPost("login")]
    public Task<Response<TokenResponse>> Login()
        => proxy.ProxyAsync<TokenResponse>(HttpContext);

    [HttpPost("refresh")]
    public Task<Response<TokenResponse>> Refresh()
        => proxy.ProxyAsync<TokenResponse>(HttpContext);

    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        await proxy.ProxyAsync<object>(HttpContext);
        return Ok();
    }
}