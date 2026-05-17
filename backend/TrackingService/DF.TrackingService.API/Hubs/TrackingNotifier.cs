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
        return hubContext.Clients
            .Group(groupName)
            .SendAsync("TrackingSnapshotUpdated", snapshot, cancellationToken);
    }
}
