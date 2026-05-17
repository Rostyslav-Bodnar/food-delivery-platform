using DF.Gateway.API.Attributes;
using DF.Gateway.API.Helpers;
using DF.Gateway.API.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DF.Gateway.API.Controllers.PaymentService;

[Authorize]
[ApiController]
[Route("api/payments")]
[GatewayService(ServiceType.PaymentService)]
public class PaymentsController(GatewayProxy proxy) : ControllerBase
{
    [HttpGet("{orderId:guid}")]
    public Task<DF.Contracts.Gateway.Responses.Response<object>> GetByOrderId(Guid orderId)
        => proxy.ProxyAsync<object>(HttpContext);

    [HttpPost("{paymentId:guid}/cancel")]
    public Task<DF.Contracts.Gateway.Responses.Response<object>> Cancel(Guid paymentId)
        => proxy.ProxyAsync<object>(HttpContext);

    [HttpPost("{paymentId:guid}/refunds")]
    public Task<DF.Contracts.Gateway.Responses.Response<object>> Refund(Guid paymentId)
        => proxy.ProxyAsync<object>(HttpContext);

    [HttpPost("{paymentId:guid}/cash/collect")]
    public Task<DF.Contracts.Gateway.Responses.Response<object>> CollectCash(Guid paymentId)
        => proxy.ProxyAsync<object>(HttpContext);

    [HttpGet("{paymentId:guid}/history")]
    public Task<DF.Contracts.Gateway.Responses.Response<object>> History(Guid paymentId)
        => proxy.ProxyAsync<object>(HttpContext);
}
