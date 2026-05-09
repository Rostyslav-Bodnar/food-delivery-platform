using DF.UserService.Domain.Entities;

namespace DF.UserService.Application.Repositories.Interfaces;

public interface IPayoutRepository : IRepository<PayoutRecord>
{
    Task<PayoutRecord?> GetByStripePayoutIdAsync(string payoutId, CancellationToken ct);
    Task<IReadOnlyList<PayoutRecord>> GetByBusinessAsync(Guid businessId, CancellationToken ct);
    
    Task AddAsync(PayoutRecord record, CancellationToken ct);

    Task SaveChangesAsync(CancellationToken ct);

}
