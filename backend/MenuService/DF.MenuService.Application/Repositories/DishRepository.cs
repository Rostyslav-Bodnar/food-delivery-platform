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
        return await dbContext.Dishes.AsNoTracking().ToListAsync();
    }

    public async Task<IEnumerable<Dish>> GetByBusinessIdAsync(Guid businessId)
    {
        return await dbContext.Dishes
            .AsNoTracking()
            .Where(d => d.BusinessId == businessId)
            .ToListAsync();
    }

    public async Task<Dish?> GetWithIngredientsAsync(Guid id)
    {
        return await dbContext.Dishes
            .AsNoTracking()
            .Include(d => d.Ingredients)
            .FirstOrDefaultAsync(d => d.Id == id);
    }

    public async Task<IReadOnlyList<Dish>> GetAllWithIngredientsAsync()
    {
        return await dbContext.Dishes
            .AsNoTracking()
            .Include(d => d.Ingredients)
            .ToListAsync();
    }

    public async Task<IReadOnlyList<Dish>> GetByBusinessIdWithIngredientsAsync(Guid businessId)
    {
        return await dbContext.Dishes
            .AsNoTracking()
            .Include(d => d.Ingredients)
            .Where(d => d.BusinessId == businessId)
            .ToListAsync();
    }

    public async Task<(IReadOnlyList<Dish> items, int total)> GetPagedWithIngredientsAsync(int skip, int take)
    {
        var baseQuery = dbContext.Dishes.AsNoTracking();
        var total = await baseQuery.CountAsync();
        var items = await baseQuery
            .OrderBy(d => d.Id)
            .Skip(skip)
            .Take(take)
            .Include(d => d.Ingredients)
            .ToListAsync();
        return (items, total);
    }

    public async Task<(IReadOnlyList<Dish> items, int total)> GetByBusinessIdPagedWithIngredientsAsync(Guid businessId, int skip, int take)
    {
        var baseQuery = dbContext.Dishes.AsNoTracking().Where(d => d.BusinessId == businessId);
        var total = await baseQuery.CountAsync();
        var items = await baseQuery
            .OrderBy(d => d.Id)
            .Skip(skip)
            .Take(take)
            .Include(d => d.Ingredients)
            .ToListAsync();
        return (items, total);
    }

    public async Task<Dish> Create(Dish entity)
    {
        var result = await dbContext.Dishes.AddAsync(entity);
        await dbContext.SaveChangesAsync();
        return result.Entity;
    }

    public async Task<Dish> CreateWithIngredientsAsync(Dish dish, IEnumerable<Ingredient> ingredients)
    {
        await dbContext.Dishes.AddAsync(dish);

        foreach (var ingredient in ingredients)
        {
            ingredient.DishId = dish.Id;
            await dbContext.Ingredients.AddAsync(ingredient);
        }

        // Single SaveChanges = single transaction in EF Core.
        await dbContext.SaveChangesAsync();
        return dish;
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
