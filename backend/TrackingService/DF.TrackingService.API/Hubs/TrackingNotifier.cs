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

    public Task OrderStatusChangedAsync(
        Guid orderId,
        Guid businessId,
        Guid customerId,
        Guid? courierId,
        string newStatus,
        string previousStatus,
        DateTime changedAtUtc,
        CancellationToken cancellationToken = default)
    {
        var payload = new
        {
            orderId,
            businessId,
            customerId,
            courierId,
            newStatus,
            previousStatus,
            changedAtUtc
        };

        var tasks = new List<Task>
        {
            // Existing per-order subscribers (LiveOrderTrackingModal etc.) get the
            // simple OrderStatusUpdated event for backwards compatibility.
            hubContext.Clients.Group($"order:{orderId}")
                .SendAsync("OrderStatusUpdated", newStatus, cancellationToken),
            hubContext.Clients.Group($"order:{orderId}")
                .SendAsync("OrderStatusChanged", payload, cancellationToken),

            // User-scoped subscribers (list pages) get the rich payload so they
            // can locate the order in their local state and patch/refresh it.
            hubContext.Clients.Group($"customer:{customerId}")
                .SendAsync("OrderStatusChanged", payload, cancellationToken),
            hubContext.Clients.Group($"business:{businessId}")
                .SendAsync("OrderStatusChanged", payload, cancellationToken)
        };

        if (courierId.HasValue && courierId.Value != Guid.Empty)
        {
            tasks.Add(hubContext.Clients.Group($"courier:{courierId.Value}")
                .SendAsync("OrderStatusChanged", payload, cancellationToken));
        }

        return Task.WhenAll(tasks);
    }
}
