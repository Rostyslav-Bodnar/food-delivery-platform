using DF.Contracts.Gateway.Responses;
using DF.Contracts.Gateway.Responses.Order;
using DF.Gateway.API.Attributes;
using DF.Gateway.API.Helpers;
using DF.Gateway.API.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DF.Gateway.API.Controllers.OrderService;

[Authorize]
[ApiController]
[Route("api/orders")]
[GatewayService(ServiceType.OrderService)]
public class OrderTrackingController(GatewayProxy proxy) : ControllerBase
{
    [HttpPost("{orderId:guid}/tracking-token")]
    public Task<Response<TrackingAccessTokenResponse>> CreateTrackingToken(Guid orderId)
        => proxy.ProxyAsync<TrackingAccessTokenResponse>(HttpContext);
}
