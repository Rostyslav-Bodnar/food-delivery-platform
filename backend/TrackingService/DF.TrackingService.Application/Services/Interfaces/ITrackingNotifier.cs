using DF.TrackingService.Contracts.Models;

namespace DF.TrackingService.Application.Services.Interfaces;

/// <summary>
/// Lets non-hub code (event-driven consumers, schedulers) broadcast tracking
/// snapshot updates to SignalR clients subscribed to an order. The hub
/// implementation lives in the API project; this abstraction keeps the
/// Application layer independent of SignalR.
/// </summary>
public interface ITrackingNotifier
{
    Task SnapshotUpdatedAsync(OrderTrackingSnapshotDto snapshot, CancellationToken cancellationToken = default);

    Task OrderStatusChangedAsync(
        Guid orderId,
        Guid businessId,
        Guid customerId,
        Guid? courierId,
        string newStatus,
        string previousStatus,
        DateTime changedAtUtc,
        CancellationToken cancellationToken = default);

    Task OrderCourierPaidAsync(
        Guid orderId,
        Guid businessId,
        Guid customerId,
        Guid courierId,
        DateTime paidAtUtc,
        CancellationToken cancellationToken = default);
}
