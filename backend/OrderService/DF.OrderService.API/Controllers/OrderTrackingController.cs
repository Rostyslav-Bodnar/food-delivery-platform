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

        var userId = userContext.UserId;
        var role = userContext.Role ?? string.Empty;

        var allowed = role switch
        {
            "Customer" => order.OrderedBy == userId,
            "Courier"  => order.DeliveredById == userId,
            "Business" => order.BusinessId == userId,
            _ => false
        };

        if (!allowed)
            return Forbid();

        var scopes = role == "Courier"
            ? new[] { "tracking:read", "tracking:write" }
            : new[] { "tracking:read" };

        var lifetime = TimeSpan.FromMinutes(10);

        var token = trackingTokenService.CreateTrackingToken(
            subjectId: userId,
            role: role,
            orderId: orderId,
            scopes: scopes,
            lifetime: lifetime);

        return Ok(token);
    }
}
