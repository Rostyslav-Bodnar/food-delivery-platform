using DF.Contracts.Gateway.Responses.Order;
using DF.OrderService.API.Middlewares;
using DF.OrderService.Application.Repositories.Interfaces;
using DF.OrderService.Application.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace DF.OrderService.API.Controllers;

[ApiController]
[Route("api/orders")]
public sealed class OrderTrackingController(
    IOrderRepository orderRepository,
    ITrackingTokenService trackingTokenService,
    IUserContext userContext)
    : ControllerBase
{
    [HttpPost("{orderId:guid}/tracking-token")]
    public async Task<ActionResult<TrackingAccessTokenResponse>> CreateTrackingToken(
        Guid orderId,
        CancellationToken ct)
    {
        if (!userContext.IsAuthenticated)
            return Unauthorized();

        var order = await orderRepository.Get(orderId);
        if (order is null)
            return NotFound();

        var accountId = userContext.AccountId;
        var accountType = userContext.AccountType ?? string.Empty;

        var allowed = accountType switch
        {
            "Customer" => order.OrderedBy == accountId,
            "Courier"  => order.DeliveredById == accountId,
            "Business" => order.BusinessId == accountId,
            _ => false
        };

        if (!allowed)
            return Forbid();

        var scopes = accountType == "Courier"
            ? new[] { "tracking:read", "tracking:write" }
            : new[] { "tracking:read" };

        var lifetime = TimeSpan.FromMinutes(10);

        var token = trackingTokenService.CreateTrackingToken(
            subjectId: accountId,
            accountType: accountType,
            orderId: orderId,
            accountId: accountId,
            scopes: scopes,
            lifetime: lifetime);

        return Ok(token);
    }
}
