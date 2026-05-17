using DF.TrackingService.Contracts.Models;

namespace DF.TrackingService.Application.Services.Interfaces;

/// <summary>
/// Persists and retrieves OrderTrackingSnapshotDto values in Redis. Shared by
/// the SignalR hub (live updates from couriers) and event-driven consumers
/// (terminal state transitions from orders being delivered or cancelled).
/// </summary>
public interface IOrderTrackingSnapshotStore
{
    Task<OrderTrackingSnapshotDto> ReadAsync(Guid orderId, CancellationToken cancellationToken = default);
    Task SaveAsync(OrderTrackingSnapshotDto snapshot, CancellationToken cancellationToken = default);
}
