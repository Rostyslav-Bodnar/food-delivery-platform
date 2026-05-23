using DF.OrderService.Contracts.Exceptions;
using DF.OrderService.Domain.Entities;

namespace DF.OrderService.Application.Services;

public static class OrderStatusTransitions
{
    // Courier-delivered orders go through the full pickup-by-courier flow.
    private static readonly Dictionary<OrderStatus, HashSet<OrderStatus>> DeliveryAllowed = new()
    {
        [OrderStatus.Preparing]      = new() { OrderStatus.Ready, OrderStatus.Canceled },
        [OrderStatus.Ready]          = new() { OrderStatus.OutForDelivery, OrderStatus.Canceled },
        [OrderStatus.OutForDelivery] = new() { OrderStatus.PickedUp, OrderStatus.Canceled },
        [OrderStatus.PickedUp]       = new() { OrderStatus.Delivered, OrderStatus.Canceled },
        [OrderStatus.Delivered]      = new(),
        [OrderStatus.Canceled]       = new(),
    };

    // Customer-pickup orders have no courier — the business marks them Delivered
    // directly from Ready when the customer collects the food.
    private static readonly Dictionary<OrderStatus, HashSet<OrderStatus>> PickupAllowed = new()
    {
        [OrderStatus.Preparing] = new() { OrderStatus.Ready, OrderStatus.Canceled },
        [OrderStatus.Ready]     = new() { OrderStatus.Delivered, OrderStatus.Canceled },
        [OrderStatus.Delivered] = new(),
        [OrderStatus.Canceled]  = new(),
    };

    public static bool IsAllowed(Order order, OrderStatus to)
    {
        var rules = order.DeliveryMethod == DeliveryMethod.Pickup
            ? PickupAllowed
            : DeliveryAllowed;
        return order.OrderStatus == to
            || rules.GetValueOrDefault(order.OrderStatus, [])!.Contains(to);
    }

    public static void EnsureAllowed(Order order, OrderStatus to)
    {
        if (!IsAllowed(order, to))
            throw new IllegalStatusTransitionException(order.OrderStatus.ToString(), to.ToString());
    }
}
