using System.Text.Json;
using DF.TrackingService.Contracts.Models;
using Microsoft.AspNetCore.SignalR;
using StackExchange.Redis;
using Microsoft.AspNetCore.Authorization;

namespace DF.TrackingService.API.Hubs;

[Authorize]
public class CourierTrackingHub(IConnectionMultiplexer redis) : Hub
{
    private readonly IDatabase _redis = redis.GetDatabase();

    // -------------------------
    // COURIER → SEND LOCATION
    // -------------------------
    public async Task SendLocation(CourierLocationDto dto)
    {

        var role = GetRole();
        var tokenOrderId = GetOrderIdClaim();

        if (role != "Courier")
            throw new HubException("Only courier can send location");

        if (!HasScope("tracking:write"))
            throw new HubException("Write scope required");

        if (tokenOrderId != dto.OrderId)
            throw new HubException("Order mismatch");

        var key = $"order:{dto.OrderId}:courier";

        await _redis.StringSetAsync(
            key,
            JsonSerializer.Serialize(dto),
            expiry: TimeSpan.FromSeconds(120)
        );


        // ✅ Broadcast клієнту + бізнесу
        await Clients.Group($"order:{dto.OrderId}")
            .SendAsync("CourierLocationUpdated", dto);
    }

    // -------------------------
    // CLIENT / BUSINESS → SUBSCRIBE
    // -------------------------
    public async Task SubscribeToOrder(Guid orderId)
    {
        var tokenOrderId = GetOrderIdClaim();

        if (tokenOrderId != orderId)
            throw new HubException("Access denied for this order");

        await Groups.AddToGroupAsync(Context.ConnectionId, $"order:{orderId}");

        // ✅ віддаємо last known location
        var key = $"order:{orderId}:courier";
        var last = await _redis.StringGetAsync(key);

        if (last.HasValue)
        {
            var json = last.ToString();
            var dto = JsonSerializer.Deserialize<CourierLocationDto>(json);
            if (dto != null)
            {
                await Clients.Caller.SendAsync("CourierLocationUpdated", dto);
            }
        }
    }

    public async Task UnsubscribeFromOrder(Guid orderId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"order:{orderId}");
    }
    private Guid GetOrderIdClaim()
    {
        var value = Context.User?.FindFirst("order_id")?.Value;
        return value != null ? Guid.Parse(value) : Guid.Empty;
    }

    private string? GetRole()
    {
        return Context.User?.FindFirst("role")?.Value;
    }

    private bool HasScope(string scope)
    {
        var scopes = Context.User?.FindFirst("scope")?.Value;
        return scopes?.Split(' ').Contains(scope) == true;
    }
}
