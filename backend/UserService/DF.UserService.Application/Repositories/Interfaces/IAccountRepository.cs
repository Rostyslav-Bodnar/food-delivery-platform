using DF.UserService.Domain.Entities;

namespace DF.UserService.Application.Repositories.Interfaces;

public interface IAccountRepository : IRepository<Account>
{
    Task<Account?> GetAccountByUserAsync(Guid userId);
    Task<IEnumerable<Account?>> GetAccountsByUserAsync(Guid userId);
    Task<Account?> GetCurrentAccountByUserId(Guid userId);

    Task<BusinessAccount?> GetBusinessByStripeIdAsync(string stripeId, CancellationToken cancellationToken);

    Task<IReadOnlyList<BusinessAccount>> GetPendingStripeAccountsAsync(
        int take,
        int maxAttempts,
        DateTime notAttemptedSinceUtc,
        CancellationToken cancellationToken);
}