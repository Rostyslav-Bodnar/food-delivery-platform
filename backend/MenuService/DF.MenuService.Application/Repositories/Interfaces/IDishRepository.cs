using DF.MenuService.Domain.Entities;

namespace DF.MenuService.Application.Repositories.Interfaces;

public interface IDishRepository : IRepository<Dish>
{
    Task<IEnumerable<Dish>> GetByBusinessIdAsync(Guid businessId);
}