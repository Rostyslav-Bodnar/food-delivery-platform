using DF.UserService.Domain.Entities;

namespace DF.UserService.Application.Repositories.Interfaces;

public interface IAccountRepository : IRepository<Account>
{
    Task<Account?> GetAccountByUserAsync(Guid userId);
    Task<IEnumerable<Account?>> GetAccountsByUserAsync(Guid userId);
}