using DF.TrackingService.Application.Services.Interfaces;
using DF.TrackingService.Contracts.Models;
using Microsoft.AspNetCore.SignalR;

namespace DF.TrackingService.API.Hubs;

/// <summary>
/// Broadcasts tracking-snapshot updates from outside the hub (event-driven
/// consumers, background workers) to the SignalR group for that order.
/// </summary>
public sealed class TrackingNotifier(IHubContext<CourierTrackingHub> hubContext) : ITrackingNotifier
{
    public Task SnapshotUpdatedAsync(OrderTrackingSnapshotDto snapshot, CancellationToken cancellationToken = default)
    {
        var groupName = $"order:{snapshot.OrderId}";

        var group = hubContext.Clients.Group(groupName);
        var tasks = new List<Task>
        {
            group.SendAsync("TrackingSnapshotUpdated", snapshot, cancellationToken)
        };

        if (snapshot.CourierLocation is not null)
        {
            tasks.Add(group.SendAsync("CourierLocationUpdated", snapshot.CourierLocation, cancellationToken));
        }

        if (!string.IsNullOrWhiteSpace(snapshot.OrderStatus))
        {
            tasks.Add(group.SendAsync("OrderStatusUpdated", snapshot.OrderStatus, cancellationToken));
        }

        return Task.WhenAll(tasks);
    }
}
