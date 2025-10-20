using DF.MenuService.Application.Repositories.Interfaces;
using DF.MenuService.Domain.Entities;
using DF.MenuService.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace DF.MenuService.Application.Repositories;

public class DishRepository(AppDbContext dbContext) : IDishRepository
{
    public async Task<Dish?> Get(Guid id)
    {
        return await dbContext.Dishes.FindAsync(id);
    }

    public async Task<IEnumerable<Dish?>> GetAll()
    {
        return await dbContext.Dishes.ToListAsync();
    }

    public async Task<IEnumerable<Dish>> GetByBusinessIdAsync(Guid businessId)
    {
        return await dbContext.Dishes.Where(d => d.BusinessId == businessId).ToListAsync();
    }


    public async Task<Dish> Create(Dish entity)
    {
        await dbContext.Dishes.AddAsync(entity);
        await dbContext.SaveChangesAsync();
        return entity;
    }

    public async Task<Dish> Update(Dish entity)
    {
        dbContext.Dishes.Update(entity);
        await dbContext.SaveChangesAsync();
        return entity;

    }


    public async Task<bool> Delete(Guid id)
    {
        var dish = await dbContext.Dishes.FindAsync(id);
        if (dish == null)
            return false;

        dbContext.Dishes.Remove(dish);
        await dbContext.SaveChangesAsync();
        return true;
    }
}