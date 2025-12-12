using System.Data.Entity;
using DF.UserService.Application.Repositories.Interfaces;
using DF.UserService.Domain.Entities;
using DF.UserService.Infrastructure.Data;

namespace DF.UserService.Application.Repositories;

public class UserRepository(AppDbContext dbContext) : IUserRepository
{
    public async Task<User?> Get(Guid id)
    {
        return await dbContext.Users.FindAsync(id);
    }

    public async Task<IEnumerable<User?>> GetAll()
    {
        return await dbContext.Users.ToListAsync();
    }

    public async Task<User> Create(User entity)
    {
        throw new NotImplementedException();
    }

    public async Task<User> Update(User entity)
    {
        throw new NotImplementedException();
    }

    public async Task<bool> Delete(Guid id)
    {
        throw new NotImplementedException();
    }
}