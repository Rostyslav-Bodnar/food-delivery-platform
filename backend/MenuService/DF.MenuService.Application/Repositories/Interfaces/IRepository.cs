using DF.MenuService.Domain.Entities;

namespace DF.MenuService.Application.Repositories.Interfaces;

public interface IRepository<T>
{
    Task<T?> Get(Guid id);
    Task<IEnumerable<T?>> GetAll();
    Task<T> Create(T entity);
    Task<Dish> Update(T entity);
    Task<bool> Delete(Guid id);
}