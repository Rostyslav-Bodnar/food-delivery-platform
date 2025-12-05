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
        return await dbContext.Orders.ToListAsync();
    }

    public async Task<Order> Create(Order entity)
    {
        var result = await dbContext.Orders.AddAsync(entity);
        await dbContext.SaveChangesAsync();
        return result.Entity;
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

    public async Task<IEnumerable<Order>> GetOrdersByUserIdAsync(Guid userId)
    {
        var orders = await dbContext.Orders.Where(o => o.DeliveredBy == userId).ToListAsync();
        return orders;
    }

    public async Task<IEnumerable<Order>> GetOrdersByBusinessIdAsync(Guid businessId)
    {
        var orders = await dbContext.Orders.Where(o => o.BusinessId == businessId).ToListAsync();
        return orders;
    }
}