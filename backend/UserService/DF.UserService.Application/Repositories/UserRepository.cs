using DF.UserService.Application.Repositories.Interfaces;
using DF.UserService.Domain.Entities;
using DF.UserService.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace DF.UserService.Application.Repositories;

public class UserRepository(AppDbContext dbContext) : IUserRepository
{
    public async Task<User?> Get(Guid id)
    {
        return await dbContext.Users.FindAsync(id);
    }

    public async Task Update(User user)
    {
        dbContext.Users.Update(user);
        await dbContext.SaveChangesAsync();
    }

    public async Task<IEnumerable<User>> GetByIds(IEnumerable<Guid> ids)
    {
        return await dbContext.Users.Where(u => ids.Contains(u.Id)).ToListAsync();
    }

}
