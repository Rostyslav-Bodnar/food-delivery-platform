using DF.UserService.Application.Repositories.Interfaces;
using DF.UserService.Domain.Entities;
using DF.UserService.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace DF.UserService.Application.Repositories;

public class AccountRepository(AppDbContext dbContext) : IAccountRepository
{
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

    public async Task<Account> Create(Account entity)
    {
        await dbContext.Accounts.AddAsync(entity);
        await dbContext.SaveChangesAsync();
        return entity;
    }

    public async Task<Account> Update(Account entity)
    {
        // Callers load the entity via Get(...) before mutating, so the change
        // tracker already has the deltas. Calling DbSet.Update(entity) would
        // mark every property modified — including Stripe-managed fields like
        // StripeChargesEnabled — and could clobber state that the webhook
        // controllers or provisioning worker just wrote.
        if (dbContext.Entry(entity).State == EntityState.Detached)
        {
            dbContext.Accounts.Attach(entity);
        }

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

    public async Task<Account?> GetCurrentAccountByUserId(Guid userId)
    {
        return await dbContext.Users
            .Include(u => u.CurrentAccount)
            .Where(u => u.Id == userId)
            .Select(u => u.CurrentAccount)
            .FirstOrDefaultAsync();
    }

    public async Task<BusinessAccount?> GetBusinessByStripeIdAsync(string stripeId, CancellationToken cancellationToken)
    {
        return await dbContext.Accounts
            .OfType<BusinessAccount>()
            .Include(a => a.User)
            .FirstOrDefaultAsync(b => b.StripeAccountId == stripeId, cancellationToken);
    }

    public async Task<IReadOnlyList<BusinessAccount>> GetPendingStripeAccountsAsync(
        int take,
        int maxAttempts,
        DateTime notAttemptedSinceUtc,
        CancellationToken cancellationToken)
    {
        return await dbContext.Accounts
            .OfType<BusinessAccount>()
            .Include(a => a.User)
            .Where(b => b.StripeAccountId == null
                        && b.StripeProvisioningAttempts < maxAttempts
                        && (b.StripeProvisioningLastAttemptUtc == null
                            || b.StripeProvisioningLastAttemptUtc < notAttemptedSinceUtc))
            .OrderBy(b => b.StripeProvisioningLastAttemptUtc ?? DateTime.MinValue)
            .ThenBy(b => b.Id)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<BusinessAccount>> GetBusinessAccountsByIds(List<Guid> ids)
    {
        return await dbContext.Accounts
            .OfType<BusinessAccount>()
            .Include(a => a.User)
            .Where(a => ids.Contains(a.Id))
            .ToListAsync();
    }
    public async Task<List<CourierAccount>> GetCourierAccountsByIds(List<Guid> ids)
    {
        return await dbContext.Accounts
            .OfType<CourierAccount>()
            .Where(a => ids.Contains(a.Id))
            .ToListAsync();
    }
    
    public async Task<List<CustomerAccount>> GetCustomerAccountsByIds(List<Guid> ids)
    {
        return await dbContext.Accounts
            .OfType<CustomerAccount>()
            .Where(a => ids.Contains(a.Id))
            .ToListAsync();
    }

}