namespace DF.TrackingService.Contracts.Models;

public record OrderTrackingSnapshotDto(
    Guid OrderId,
    Guid? CourierId,
    string Stage,
    CourierLocationDto? CourierLocation,
    string? OrderStatus,
    DateTime UpdatedAtUtc
);

public static class OrderTrackingStages
{
    public const string AwaitingCourier = "awaiting-courier";
    public const string ToRestaurant = "to-restaurant";
    public const string ToCustomer = "to-customer";
    public const string Delivered = "delivered";
    public const string Cancelled = "cancelled";

    private static readonly HashSet<string> ValidStages = new(StringComparer.OrdinalIgnoreCase)
    {
        AwaitingCourier,
        ToRestaurant,
        ToCustomer,
        Delivered,
        Cancelled
    };

    public static bool IsValid(string? stage)
    {
        return !string.IsNullOrWhiteSpace(stage) && ValidStages.Contains(stage);
    }

    public static string Normalize(string? stage)
    {
        if (string.IsNullOrWhiteSpace(stage))
        {
            return AwaitingCourier;
        }

        return stage.Trim().ToLowerInvariant() switch
        {
            AwaitingCourier => AwaitingCourier,
            ToRestaurant => ToRestaurant,
            ToCustomer => ToCustomer,
            Delivered => Delivered,
            Cancelled => Cancelled,
            _ => AwaitingCourier
        };
    }
}
