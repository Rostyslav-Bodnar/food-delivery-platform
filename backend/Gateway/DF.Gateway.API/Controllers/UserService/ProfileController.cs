using DF.Contracts.Gateway.Responses;
using DF.Gateway.API.Attributes;
using DF.Gateway.API.Helpers;
using DF.Gateway.API.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DF.Gateway.API.Controllers.UserService;

[Authorize]
[ApiController]
[Route("api/[controller]")]
[GatewayService(ServiceType.UserService)]
public class ProfileController(GatewayProxy proxy) : ControllerBase
{
    // =========================
    // GET PROFILE
    // =========================
    [HttpGet]
    public Task<Response<ProfileResponse>> GetProfile()
        => proxy.ProxyAsync<ProfileResponse>(HttpContext);

    // =========================
    // SWITCH ACCOUNT
    // =========================
    [HttpPut("switch/{accountId:guid}")]
    public Task<Response<TokenResponse>> SwitchAccount(Guid accountId)
        => proxy.ProxyAsync<TokenResponse>(HttpContext);
}