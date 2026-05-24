using DF.OrderService.Application.Repositories.Interfaces;
using DF.OrderService.Domain.Entities;
using DF.OrderService.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace DF.OrderService.Application.Repositories;

public class OrderRepository(AppDbContext dbContext) : IOrderRepository
{
    public async Task<Order?> Get(Guid id)
    {
        return await dbContext.Orders.FindAsync(id);
    }

    public async Task<IEnumerable<Order?>> GetAll()
    {
        return await dbContext.Orders.AsNoTracking().Include(o => o.OrderedDishes).ToListAsync();
    }

    public async Task<Order> Create(Order entity)
    {
        var result = await dbContext.Orders.AddAsync(entity);
        await dbContext.SaveChangesAsync();
        return result.Entity;
    }

    public async Task<Order> CreateWithDishesAsync(Order order, IEnumerable<OrderedDish> dishes)
    {
        await dbContext.Orders.AddAsync(order);

        foreach (var dish in dishes)
        {
            dish.OrderId = order.Id;
            await dbContext.OrderedDishes.AddAsync(dish);
        }

        await dbContext.SaveChangesAsync();
        return order;
    }

    public async Task<Order> Update(Order entity)
    {
        var result = dbContext.Orders.Update(entity);
        await dbContext.SaveChangesAsync();
        return result.Entity;
    }

    public async Task<bool> Delete(Guid id)
    {
        var order = await dbContext.Orders.FindAsync(id);
        if (order == null)
            return false;

        dbContext.Orders.Remove(order);
        await dbContext.SaveChangesAsync();
        return true;
    }

    public async Task<IEnumerable<Order>> GetOrdersByCustomerIdAsync(Guid customerId)
    {
        return await dbContext.Orders
            .AsNoTracking()
            .Include(o => o.OrderedDishes)
            .Where(o => o.OrderedBy == customerId)
            .ToListAsync();
    }

    public async Task<IEnumerable<Order>> GetOrdersByBusinessIdAsync(Guid businessId)
    {
        return await dbContext.Orders
            .AsNoTracking()
            .Include(o => o.OrderedDishes)
            .Where(o => o.BusinessId == businessId)
            .ToListAsync();
    }

    public async Task<IEnumerable<Order>> GetOrdersByCourierIdAsync(Guid courierId)
    {
        return await dbContext.Orders
            .AsNoTracking()
            .Include(o => o.OrderedDishes)
            .Where(o => o.DeliveredById == courierId)
            .ToListAsync();
    }

    public async Task<Order?> GetWithDishesAsync(Guid id)
    {
        return await dbContext.Orders
            .AsNoTracking()
            .Include(o => o.OrderedDishes)
            .FirstOrDefaultAsync(o => o.Id == id);
    }

    public async Task<(IReadOnlyList<Order> items, int total)> GetAllPagedAsync(int skip, int take)
    {
        var baseQuery = dbContext.Orders.AsNoTracking();
        var total = await baseQuery.CountAsync();
        var items = await baseQuery
            .OrderByDescending(o => o.OrderDate)
            .Skip(skip)
            .Take(take)
            .Include(o => o.OrderedDishes)
            .ToListAsync();
        return (items, total);
    }
    public async Task<IReadOnlyList<Order>> GetDeliveredOrdersByBusinessInWindowAsync(
        Guid businessId,
        DateTime fromUtc,
        DateTime toUtc,
        CancellationToken ct = default)
    {
        return await dbContext.Orders
            .AsNoTracking()
            .Include(o => o.OrderedDishes)
            .Where(o => o.BusinessId == businessId
                        && o.OrderStatus == OrderStatus.Delivered
                        && o.OrderDate >= fromUtc
                        && o.OrderDate <= toUtc)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<Order>> GetStaleUnpaidOnlineOrdersAsync(
        DateTime cutoffUtc,
        int take,
        CancellationToken ct = default)
    {
        // Only cancel orders that haven't gone out the door yet. Once a
        // courier has picked the order up (PickedUp/OutForDelivery), the
        // restaurant has already invested cost — at that point the cancel
        // should be operator-driven, not automated.
        return await dbContext.Orders
            .Where(o => o.PaymentMethod == PaymentMethod.Online
                        && !o.IsPaid
                        && o.OrderDate < cutoffUtc
                        && (o.OrderStatus == OrderStatus.Preparing
                            || o.OrderStatus == OrderStatus.Ready))
            .OrderBy(o => o.OrderDate)
            .Take(take)
            .ToListAsync(ct);
    }

    public async Task<bool> CreateRangeWithDishesAsync(
        IEnumerable<Order> orders,
        IEnumerable<OrderedDish> dishes)
    {
        if (orders == null || !orders.Any())
            return false;

        var orderList = orders.ToList();
        var dishList = dishes.ToList();

        // 1. Add all orders in one batch
        await dbContext.Orders.AddRangeAsync(orderList);

        // 2. Map dishes to orders (O(1) lookup, no nested loops)
        var orderIds = orderList.ToDictionary(x => x.Id);

        foreach (var dish in dishList)
        {
            if (dish.OrderId == Guid.Empty)
                throw new InvalidOperationException("OrderedDish must have OrderId before saving");

            if (!orderIds.ContainsKey(dish.OrderId))
                throw new InvalidOperationException($"Order {dish.OrderId} not found in batch");

            dbContext.OrderedDishes.Add(dish);
        }

        // 3. Single DB transaction
        await dbContext.SaveChangesAsync();

        return true;
    }
}
