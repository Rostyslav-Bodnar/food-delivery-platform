using DF.Gateway.API.Attributes;
using DF.Gateway.API.Helpers;
using DF.Gateway.API.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DF.Gateway.API.Controllers.PaymentService;

[Authorize]
[ApiController]
[Route("api/courier-payouts")]
[GatewayService(ServiceType.PaymentService)]
public class CourierPayoutsController(GatewayProxy proxy) : ControllerBase
{
    [HttpGet("{courierId:guid}/balance")]
    public Task<DF.Contracts.Gateway.Responses.Response<object>> GetBalance(Guid courierId)
        => proxy.ProxyAsync<object>(HttpContext);

    [HttpPut("{courierId:guid}/account")]
    public Task<DF.Contracts.Gateway.Responses.Response<object>> UpsertAccount(Guid courierId)
        => proxy.ProxyAsync<object>(HttpContext);
}
