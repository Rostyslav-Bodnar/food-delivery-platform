using DF.OrderService.Domain.Entities;

namespace DF.OrderService.Application.Repositories.Interfaces;

public interface IOrderDishRepository : IRepository<OrderedDish>
{
    Task<IEnumerable<OrderedDish>> GetOrderDishesByOrderId(Guid orderId);
}