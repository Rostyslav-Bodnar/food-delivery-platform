using DF.UserService.Domain.Entities;

namespace DF.UserService.Application.Repositories.Interfaces;

public interface IUserRepository
{
    Task<User?> Get(Guid id);
    Task Update(User user);
    Task<IEnumerable<User>> GetByIds(IEnumerable<Guid> ids);
}
