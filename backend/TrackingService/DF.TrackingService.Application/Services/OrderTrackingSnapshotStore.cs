using System.Text.Json;
using DF.TrackingService.Application.Services.Interfaces;
using DF.TrackingService.Contracts.Models;
using StackExchange.Redis;

namespace DF.TrackingService.Application.Services;

public sealed class OrderTrackingSnapshotStore(IConnectionMultiplexer redis) : IOrderTrackingSnapshotStore
{
    private const string GroupPrefix = "order:";
    private static readonly TimeSpan SnapshotExpiry = TimeSpan.FromHours(6);

    private readonly IDatabase _redis = redis.GetDatabase();

    public async Task<OrderTrackingSnapshotDto> ReadAsync(Guid orderId, CancellationToken cancellationToken = default)
    {
        var json = await _redis.StringGetAsync(GetSnapshotKey(orderId));

        if (json.HasValue)
        {
            var snapshot = JsonSerializer.Deserialize<OrderTrackingSnapshotDto>((ReadOnlySpan<byte>)json!);
            if (snapshot is not null)
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
            OrderStatus: null,
            UpdatedAtUtc: DateTime.UtcNow);
    }

    public Task SaveAsync(OrderTrackingSnapshotDto snapshot, CancellationToken cancellationToken = default)
    {
        return _redis.StringSetAsync(
            GetSnapshotKey(snapshot.OrderId),
            JsonSerializer.Serialize(snapshot),
            expiry: SnapshotExpiry);
    }

    private static string GetSnapshotKey(Guid orderId) => $"{GroupPrefix}{orderId}:tracking";
}
