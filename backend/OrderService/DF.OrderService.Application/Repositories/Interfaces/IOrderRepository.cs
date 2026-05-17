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
}
