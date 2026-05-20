using DF.MenuService.Domain.Entities;

namespace DF.MenuService.Application.Repositories.Interfaces;

public interface IDishRepository : IRepository<Dish>
{
    Task<IEnumerable<Dish>> GetByBusinessIdAsync(Guid businessId);

    // Read-optimized: AsNoTracking + ingredients eagerly loaded.
    Task<Dish?> GetWithIngredientsAsync(Guid id);
    Task<IReadOnlyList<Dish>> GetAllWithIngredientsAsync();
    Task<IReadOnlyList<Dish>> GetByBusinessIdWithIngredientsAsync(Guid businessId);

    // Paged variants — push OFFSET/LIMIT to SQL.
    Task<(IReadOnlyList<Dish> items, int total)> GetPagedWithIngredientsAsync(int skip, int take);
    Task<(IReadOnlyList<Dish> items, int total)> GetByBusinessIdPagedWithIngredientsAsync(Guid businessId, int skip, int take);

    // Single-transaction insert: dish + ingredients commit together or not at all.
    Task<Dish> CreateWithIngredientsAsync(Dish dish, IEnumerable<Ingredient> ingredients);
}
