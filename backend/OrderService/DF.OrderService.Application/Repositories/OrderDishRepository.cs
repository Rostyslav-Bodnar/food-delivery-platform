using DF.OrderService.Application.Repositories.Interfaces;
using DF.OrderService.Domain.Entities;
using DF.OrderService.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace DF.OrderService.Application.Repositories;

public class OrderDishRepository(AppDbContext dbContext) : IOrderDishRepository
{
    public async Task<OrderedDish?> Get(Guid id)
    {
        return await dbContext.OrderedDishes.FindAsync(id);
    }

    public async Task<IEnumerable<OrderedDish?>> GetAll()
    {
        return await dbContext.OrderedDishes.ToListAsync();
    }

    public async Task<OrderedDish> Create(OrderedDish entity)
    {
        var result = await dbContext.OrderedDishes.AddAsync(entity);
        await dbContext.SaveChangesAsync();
        return result.Entity;
    }

    public async Task<OrderedDish> Update(OrderedDish entity)
    {
        var result = dbContext.OrderedDishes.Update(entity);
        await dbContext.SaveChangesAsync();
        return result.Entity;
    }

    public async Task<bool> Delete(Guid id)
    {
        var orderDish = await dbContext.OrderedDishes.FindAsync(id);
        if (orderDish == null)
            return false;

        dbContext.OrderedDishes.Remove(orderDish);
        await dbContext.SaveChangesAsync();
        return true;
    }

    public async Task<IEnumerable<OrderedDish>> GetOrderDishesByOrderId(Guid orderId)
    {
        return await dbContext.OrderedDishes.Where(o => o.OrderId == orderId).ToListAsync();
    }
}