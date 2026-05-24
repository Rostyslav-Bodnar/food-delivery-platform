using DF.OrderService.Domain.Entities;

namespace DF.OrderService.Application.Repositories.Interfaces;

public interface IOrderRepository : IRepository<Order>
{
    Task<IEnumerable<Order>> GetOrdersByCustomerIdAsync(Guid customerId);
    Task<IEnumerable<Order>> GetOrdersByBusinessIdAsync(Guid businessId);
    Task<IEnumerable<Order>> GetOrdersByCourierIdAsync(Guid courierId);

    // Single-transaction insert: order + its ordered dishes commit together or not at all.
    Task<Order> CreateWithDishesAsync(Order order, IEnumerable<OrderedDish> dishes);

    Task<(IReadOnlyList<Order> items, int total)> GetAllPagedAsync(int skip, int take);

    Task<Order?> GetWithDishesAsync(Guid id);
    Task<bool> CreateRangeWithDishesAsync(
        IEnumerable<Order> orders,
        IEnumerable<OrderedDish> dishes);

    /// <summary>
    /// Delivered orders for a business inside [fromUtc, toUtc], with their
    /// OrderedDishes included. Used by the dashboard's per-dish revenue
    /// aggregation.
    /// </summary>
    Task<IReadOnlyList<Order>> GetDeliveredOrdersByBusinessInWindowAsync(
        Guid businessId,
        DateTime fromUtc,
        DateTime toUtc,
        CancellationToken ct = default);

    /// <summary>
    /// Online (card-paid) orders that are still in pre-pickup state, haven't
    /// been paid, and were created before `cutoffUtc`. Used by the
    /// payment-timeout worker to auto-cancel abandoned orders that never
    /// got paid.
    /// </summary>
    Task<IReadOnlyList<Order>> GetStaleUnpaidOnlineOrdersAsync(
        DateTime cutoffUtc,
        int take,
        CancellationToken ct = default);
}
