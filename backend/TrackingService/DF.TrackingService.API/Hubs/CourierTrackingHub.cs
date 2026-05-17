using DF.TrackingService.Application.Services.Interfaces;
using DF.TrackingService.Contracts.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace DF.TrackingService.API.Hubs;

[Authorize(AuthenticationSchemes = "TrackingHub")]
public class CourierTrackingHub(IOrderTrackingSnapshotStore snapshotStore) : Hub
{
    private const string GroupPrefix = "order:";

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

        var snapshot = await snapshotStore.ReadAsync(dto.OrderId);
        snapshot = snapshot with
        {
            CourierId = dto.CourierId,
            Stage = OrderTrackingStages.Normalize(snapshot.Stage),
            CourierLocation = dto,
            UpdatedAtUtc = dto.TimestampUtc == default ? DateTime.UtcNow : dto.TimestampUtc
        };

        await snapshotStore.SaveAsync(snapshot);
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

        var snapshot = await snapshotStore.ReadAsync(orderId);
        snapshot = snapshot with
        {
            CourierId = snapshot.CourierId ?? GetSubjectId(),
            Stage = OrderTrackingStages.Normalize(stage),
            UpdatedAtUtc = DateTime.UtcNow
        };

        await snapshotStore.SaveAsync(snapshot);
        await BroadcastSnapshotAsync(snapshot);
    }

    public async Task SubscribeToOrder(Guid orderId)
    {
        var tokenOrderId = GetOrderIdClaim();

        if (tokenOrderId != orderId)
            throw new HubException("Access denied for this order");

        await Groups.AddToGroupAsync(Context.ConnectionId, GetGroupName(orderId));

        var snapshot = await snapshotStore.ReadAsync(orderId);
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

    private static string GetGroupName(Guid orderId) => $"{GroupPrefix}{orderId}";
}
