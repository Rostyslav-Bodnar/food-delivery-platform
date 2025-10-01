using DF.UserService.Application.Repositories.Interfaces;
using DF.UserService.Domain.Entities;
using DF.UserService.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace DF.UserService.Application.Repositories;

public class AccountRepository : IAccountRepository
{
    private readonly AppDbContext dbContext;

    public AccountRepository(AppDbContext dbContext)
    {
        this.dbContext = dbContext;
    }
    
    public async Task<Account?> Get(Guid id)
    {
        return await dbContext.Accounts
            .Include(a => a.User)
            .FirstOrDefaultAsync(a => a.Id == id);
    }

    public async Task<IEnumerable<Account?>> GetAll()
    {
        return await dbContext.Accounts
            .Include(a => a.User)
            .ToListAsync();
    }

    public async Task<bool> Create(Account entity)
    {
        await dbContext.Accounts.AddAsync(entity);
        return await dbContext.SaveChangesAsync() > 0;
    }

    public async Task<Account> Update(Account entity)
    {
        dbContext.Accounts.Update(entity);
        await dbContext.SaveChangesAsync();
        return entity;
    }


    public async Task<bool> Delete(Guid id)
    {
        var account = await dbContext.Accounts.FindAsync(id);
        if (account == null) return false;

        dbContext.Accounts.Remove(account);
        return await dbContext.SaveChangesAsync() > 0;
    }

    public async Task<Account?> GetAccountByUserAsync(Guid userId)
    {
        return await dbContext.Accounts
            .Include(a => a.User)
            .FirstOrDefaultAsync(a => a.UserId == userId);
    }

    public async Task<IEnumerable<Account?>> GetAccountsByUserAsync(Guid userId)
    {
        return await dbContext.Accounts
            .Include(a => a.User)
            .Where(a => a.UserId == userId)
            .ToListAsync();
    }
}