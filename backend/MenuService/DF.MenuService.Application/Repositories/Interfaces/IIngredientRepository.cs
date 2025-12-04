using DF.MenuService.Domain.Entities;

namespace DF.MenuService.Application.Repositories.Interfaces;

public interface IIngredientRepository : IRepository<Ingredient>
{
    Task<IEnumerable<Ingredient>> GetAllIngredientsByDishId(Guid dishId);
    Task<IEnumerable<Ingredient>> CreateIngredients(IEnumerable<Ingredient> ingredients);
}