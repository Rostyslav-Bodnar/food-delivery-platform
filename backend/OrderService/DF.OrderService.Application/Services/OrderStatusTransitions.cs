using DF.OrderService.Contracts.Exceptions;
using DF.OrderService.Domain.Entities;

namespace DF.OrderService.Application.Services;

public static class OrderStatusTransitions
{
    // Allowed forward transitions. Canceled is reachable from any non-terminal status.
    private static readonly Dictionary<OrderStatus, HashSet<OrderStatus>> Allowed = new()
    {
        [OrderStatus.Preparing]      = new() { OrderStatus.Ready, OrderStatus.Canceled },
        [OrderStatus.Ready]          = new() { OrderStatus.OutForDelivery, OrderStatus.Canceled },
        [OrderStatus.OutForDelivery] = new() { OrderStatus.Delivered },
        [OrderStatus.Delivered]      = new(),    // terminal
        [OrderStatus.Canceled]       = new(),    // terminal
    };

    public static bool IsAllowed(OrderStatus from, OrderStatus to)
        => from == to || Allowed.GetValueOrDefault(from, [])!.Contains(to);

    public static void EnsureAllowed(OrderStatus from, OrderStatus to)
    {
        if (!IsAllowed(from, to))
            throw new IllegalStatusTransitionException(from.ToString(), to.ToString());
    }
}
