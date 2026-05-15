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
    [Consumes("multipart/form-data")]
    public Task<Response<CustomerAccountResponse>> CreateCustomer()
        => proxy.ProxyAsync<CustomerAccountResponse>(HttpContext);

    [HttpPost("business")]
    [Consumes("multipart/form-data")]
    public Task<Response<BusinessAccountResponse>> CreateBusiness()
        => proxy.ProxyAsync<BusinessAccountResponse>(HttpContext);

    [HttpPost("courier")]
    [Consumes("multipart/form-data")]
    public Task<Response<CourierAccountResponse>> CreateCourier()
        => proxy.ProxyAsync<CourierAccountResponse>(HttpContext);

    [HttpPut("customer")]
    public Task<Response<CustomerAccountResponse>> UpdateCustomer()
        => proxy.ProxyAsync<CustomerAccountResponse>(HttpContext);

    [HttpPut("business")]
    public Task<Response<BusinessAccountResponse>> UpdateBusiness()
        => proxy.ProxyAsync<BusinessAccountResponse>(HttpContext);

    [HttpPut("courier")]
    public Task<Response<CourierAccountResponse>> UpdateCourier()
        => proxy.ProxyAsync<CourierAccountResponse>(HttpContext);
}