using System.Text.Json;
using DF.TrackingService.Contracts.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using StackExchange.Redis;

namespace DF.TrackingService.API.Hubs;

[Authorize]
public class CourierTrackingHub(IConnectionMultiplexer redis) : Hub
{
    private const string GroupPrefix = "order:";
    private static readonly TimeSpan SnapshotExpiry = TimeSpan.FromHours(6);

    private readonly IDatabase _redis = redis.GetDatabase();

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

        var snapshot = await ReadSnapshotAsync(dto.OrderId);
        snapshot = snapshot with
        {
            CourierId = dto.CourierId,
            Stage = OrderTrackingStages.Normalize(snapshot.Stage),
            CourierLocation = dto,
            UpdatedAtUtc = dto.TimestampUtc == default ? DateTime.UtcNow : dto.TimestampUtc
        };

        await SaveSnapshotAsync(snapshot);
        await BroadcastSnapshotAsync(snapshot);
    }

    public async Task UpdateTrackingStage(Guid orderId, string stage)
    {
        var role = GetRole();
        var tokenOrderId = GetOrderIdClaim();

        if (role != "Courier")
            throw new HubException("Only courier can update tracking stage");

        if (!HasScope("tracking:write"))
            throw new HubException("Write scope required");

        if (tokenOrderId != orderId)
            throw new HubException("Order mismatch");

        if (!OrderTrackingStages.IsValid(stage))
            throw new HubException("Unsupported tracking stage");

        var snapshot = await ReadSnapshotAsync(orderId);
        snapshot = snapshot with
        {
            CourierId = snapshot.CourierId ?? GetSubjectId(),
            Stage = OrderTrackingStages.Normalize(stage),
            UpdatedAtUtc = DateTime.UtcNow
        };

        await SaveSnapshotAsync(snapshot);
        await BroadcastSnapshotAsync(snapshot);
    }

    public async Task SubscribeToOrder(Guid orderId)
    {
        var tokenOrderId = GetOrderIdClaim();

        if (tokenOrderId != orderId)
            throw new HubException("Access denied for this order");

        await Groups.AddToGroupAsync(Context.ConnectionId, GetGroupName(orderId));

        var snapshot = await ReadSnapshotAsync(orderId);
        await SendSnapshotToCallerAsync(snapshot);
    }

    public async Task UnsubscribeFromOrder(Guid orderId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, GetGroupName(orderId));
    }

    private Guid GetOrderIdClaim()
    {
        var value = Context.User?.FindFirst("order_id")?.Value;
        return value != null ? Guid.Parse(value) : Guid.Empty;
    }

    private Guid? GetSubjectId()
    {
        var value = Context.User?.FindFirst("sub")?.Value;
        return Guid.TryParse(value, out var subjectId) ? subjectId : null;
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

    private async Task<OrderTrackingSnapshotDto> ReadSnapshotAsync(Guid orderId)
    {
        var snapshotJson = await _redis.StringGetAsync(GetSnapshotKey(orderId));

        if (snapshotJson.HasValue)
        {
            var snapshot = JsonSerializer.Deserialize<OrderTrackingSnapshotDto>((ReadOnlySpan<byte>)snapshotJson);
            if (snapshot != null)
            {
                return snapshot with
                {
                    Stage = OrderTrackingStages.Normalize(snapshot.Stage)
                };
            }
        }

        return new OrderTrackingSnapshotDto(
            OrderId: orderId,
            CourierId: null,
            Stage: OrderTrackingStages.AwaitingCourier,
            CourierLocation: null,
            UpdatedAtUtc: DateTime.UtcNow
        );
    }

    private async Task SaveSnapshotAsync(OrderTrackingSnapshotDto snapshot)
    {
        await _redis.StringSetAsync(
            GetSnapshotKey(snapshot.OrderId),
            JsonSerializer.Serialize(snapshot),
            expiry: SnapshotExpiry);
    }

    private async Task BroadcastSnapshotAsync(OrderTrackingSnapshotDto snapshot)
    {
        await Clients.Group(GetGroupName(snapshot.OrderId))
            .SendAsync("TrackingSnapshotUpdated", snapshot);

        if (snapshot.CourierLocation != null)
        {
            await Clients.Group(GetGroupName(snapshot.OrderId))
                .SendAsync("CourierLocationUpdated", snapshot.CourierLocation);
        }
    }

    private async Task SendSnapshotToCallerAsync(OrderTrackingSnapshotDto snapshot)
    {
        await Clients.Caller.SendAsync("TrackingSnapshotUpdated", snapshot);

        if (snapshot.CourierLocation != null)
        {
            await Clients.Caller.SendAsync("CourierLocationUpdated", snapshot.CourierLocation);
        }
    }

    private static string GetSnapshotKey(Guid orderId) => $"{GetGroupName(orderId)}:tracking";

    private static string GetGroupName(Guid orderId) => $"{GroupPrefix}{orderId}";
}
