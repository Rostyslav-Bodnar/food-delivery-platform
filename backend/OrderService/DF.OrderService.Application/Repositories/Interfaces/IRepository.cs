namespace DF.OrderService.Application.Repositories.Interfaces;

public interface IRepository<T>
{
    Task<T?> Get(Guid id);
    Task<IEnumerable<T?>> GetAll();
    Task<T> Create(T entity);
    Task<T> Update(T entity);
    Task<bool> Delete(Guid id);
}