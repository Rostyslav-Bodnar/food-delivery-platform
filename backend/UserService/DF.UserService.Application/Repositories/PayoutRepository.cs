using DF.UserService.Application.Repositories.Interfaces;
using DF.UserService.Domain.Entities;
using DF.UserService.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace DF.UserService.Application.Repositories;

public class PayoutRepository(AppDbContext dbContext) : IPayoutRepository
{
    public async Task<PayoutRecord?> Get(Guid id)
    {
        return await dbContext.PayoutRecords.FirstOrDefaultAsync(p => p.Id == id);
    }

    public async Task<IEnumerable<PayoutRecord?>> GetAll()
    {
        return await dbContext.PayoutRecords.ToListAsync();
    }

    public async Task<PayoutRecord> Create(PayoutRecord entity)
    {
        await dbContext.PayoutRecords.AddAsync(entity);
        await dbContext.SaveChangesAsync();
        return entity;
    }

    public async Task<PayoutRecord> Update(PayoutRecord entity)
    {
        dbContext.PayoutRecords.Update(entity);
        await dbContext.SaveChangesAsync();
        return entity;
    }

    public async Task<bool> Delete(Guid id)
    {
        var entity = await dbContext.PayoutRecords.FindAsync(id);
        if (entity is null) return false;

        dbContext.PayoutRecords.Remove(entity);
        return await dbContext.SaveChangesAsync() > 0;
    }

    public async Task<PayoutRecord?> GetByStripePayoutIdAsync(string payoutId, CancellationToken ct)
    {
        return await dbContext.PayoutRecords
            .FirstOrDefaultAsync(p => p.StripePayoutId == payoutId, ct);
    }

    public async Task<IReadOnlyList<PayoutRecord>> GetByBusinessAsync(Guid businessId, CancellationToken ct)
    {
        return await dbContext.PayoutRecords
            .Where(p => p.BusinessId == businessId)
            .OrderByDescending(p => p.CreatedAtUtc)
            .ToListAsync(ct);
    }

    public async Task AddAsync(PayoutRecord entity, CancellationToken ct)
    {
        await dbContext.PayoutRecords.AddAsync(entity, ct);
    }

    public async Task SaveChangesAsync(CancellationToken ct)
    {
        await dbContext.SaveChangesAsync(ct);
    }
}