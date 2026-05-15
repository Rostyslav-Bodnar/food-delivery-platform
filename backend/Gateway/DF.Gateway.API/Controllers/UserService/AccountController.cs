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
public class AccountController(GatewayProxy proxy) : ControllerBase
{
    // =========================
    // GET SINGLE ACCOUNT
    // =========================
    [HttpGet("{userId:guid}")]
    public Task<Response<AccountResponse>> GetAccount(Guid userId)
        => proxy.ProxyAsync<AccountResponse>(HttpContext);

    // =========================
    // GET USER ACCOUNTS
    // =========================
    [HttpGet("all/{userId:guid}")]
    public Task<Response<IEnumerable<AccountResponse>>> GetAccounts(Guid userId)
        => proxy.ProxyAsync<IEnumerable<AccountResponse>>(HttpContext);

    // =========================
    // BUSINESS ACCOUNTS
    // =========================
    [HttpGet("all/business")]
    public Task<Response<IEnumerable<AccountResponse>>> GetAllBusiness()
        => proxy.ProxyAsync<IEnumerable<AccountResponse>>(HttpContext);

    // =========================
    // ONBOARDING
    // =========================
    [HttpGet("onboarding/{businessId:guid}")]
    public Task<Response<string>> GetOnboardingLink(Guid businessId)
        => proxy.ProxyAsync<string>(HttpContext);

    // =========================
    // CREATE / UPDATE (multipart)
    // =========================

    [HttpPost("customer")]
    public Task<Response<AccountResponse>> CreateCustomer()
        => proxy.ProxyAsync<AccountResponse>(HttpContext);

    [HttpPost("business")]
    public Task<Response<AccountResponse>> CreateBusiness()
        => proxy.ProxyAsync<AccountResponse>(HttpContext);

    [HttpPost("courier")]
    public Task<Response<AccountResponse>> CreateCourier()
        => proxy.ProxyAsync<AccountResponse>(HttpContext);

    [HttpPut("customer")]
    public Task<Response<AccountResponse>> UpdateCustomer()
        => proxy.ProxyAsync<AccountResponse>(HttpContext);

    [HttpPut("business")]
    public Task<Response<AccountResponse>> UpdateBusiness()
        => proxy.ProxyAsync<AccountResponse>(HttpContext);

    [HttpPut("courier")]
    public Task<Response<AccountResponse>> UpdateCourier()
        => proxy.ProxyAsync<AccountResponse>(HttpContext);
}