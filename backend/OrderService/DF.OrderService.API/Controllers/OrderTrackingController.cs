using DF.OrderService.Application.Repositories.Interfaces;
using DF.OrderService.Application.Services.Interfaces;
using DF.OrderService.Contracts.Models.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DF.OrderService.API.Controllers;

[ApiController]
[Route("api/orders")]
[Authorize]
public sealed class OrderTrackingController(
    IOrderRepository orderRepository,
    ITrackingTokenService trackingTokenService)
    : ControllerBase
{
    [HttpPost("{orderId:guid}/tracking-token")]
    public async Task<ActionResult<TrackingAccessTokenResponse>> CreateTrackingToken(
        Guid orderId,
        CancellationToken ct)
    {
        var order = await orderRepository.Get(orderId);
        if (order is null)
            return NotFound();

        // --------
        // Ідентифікація користувача
        // --------
        var userId = Guid.Parse(User.FindFirst("sub")!.Value);
        var role   = User.FindFirst("role")!.Value;

        // --------
        // Перевірка доступу
        // --------
        bool allowed = role switch
        {
            "Customer" => order.OrderedBy == userId,
            "Courier"  => order.DeliveredById == userId,
            "Business" => order.BusinessId == userId,
            _ => false
        };

        if (!allowed)
            return Forbid();

        // --------
        // Scopes
        // --------
        var scopes = role == "Courier"
            ? new[] { "tracking:read", "tracking:write" }
            : new[] { "tracking:read" };

        // --------
        // Token lifetime
        // --------
        var lifetime = TimeSpan.FromMinutes(10);

        var token = trackingTokenService.CreateTrackingToken(
            subjectId: userId,
            role: role,
            orderId: orderId,
            scopes: scopes,
            lifetime: lifetime
        );

        return Ok(token);
    }
}
