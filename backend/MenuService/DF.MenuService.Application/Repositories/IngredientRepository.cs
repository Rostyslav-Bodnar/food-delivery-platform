using DF.MenuService.Application.Repositories.Interfaces;
using DF.MenuService.Domain.Entities;
using DF.MenuService.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace DF.MenuService.Application.Repositories;

public class IngredientRepository(AppDbContext dbContext) : IIngredientRepository
{
    
    public async Task<Ingredient?> Get(Guid id)
    {
        return await dbContext.Ingredients.FirstOrDefaultAsync(i => i.Id == id);
    }

    public async Task<IEnumerable<Ingredient?>> GetAll()
    {
        return await dbContext.Ingredients.ToListAsync();
    }

    public async Task<Ingredient> Create(Ingredient entity)
    {
        var result = await dbContext.Ingredients.AddAsync(entity);
        await dbContext.SaveChangesAsync();
        return result.Entity;
    }

    public async Task<Ingredient> Update(Ingredient entity)
    {
        var result = dbContext.Ingredients.Update(entity);
        await dbContext.SaveChangesAsync();
        return result.Entity;
    }

    public async Task<bool> Delete(Guid id)
    {
        var ingredient = await dbContext.Ingredients.FirstOrDefaultAsync(i => i.Id == id);
        if(ingredient == null)
            return false;
        dbContext.Ingredients.Remove(ingredient);
        await dbContext.SaveChangesAsync();
        
        return true;
    }

    public async Task<IEnumerable<Ingredient>> GetAllIngredientsByDishId(Guid dishId)
    {
        return await dbContext.Ingredients.Where(i => i.DishId == dishId).ToListAsync();
    }

    public async Task<IEnumerable<Ingredient>> CreateIngredients(IEnumerable<Ingredient> ingredients)
    {
        await dbContext.Ingredients.AddRangeAsync(ingredients);
        await dbContext.SaveChangesAsync();

        return ingredients;
    }

}