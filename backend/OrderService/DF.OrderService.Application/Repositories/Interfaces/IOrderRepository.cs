using DF.OrderService.Domain.Entities;

namespace DF.OrderService.Application.Repositories.Interfaces;

public interface IOrderRepository : IRepository<Order>
{
    Task<IEnumerable<Order>> GetOrdersByCustomerIdAsync(Guid customerId);
    Task<IEnumerable<Order>> GetOrdersByBusinessIdAsync(Guid businessId);
    Task<IEnumerable<Order>> GetOrdersByCourierIdAsync(Guid courierId);

}