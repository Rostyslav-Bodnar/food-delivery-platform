using DF.PaymentService.Application.Repositories.Interfaces;
using DF.PaymentService.Domain.Entities;
using DF.PaymentService.Infrastructure.Data;

namespace DF.PaymentService.Infrastructure.Repositories;

public class PaymentTaskRepository(AppDbContext dbContext) : IPaymentTaskRepository
{
    public async Task AddAsync(PaymentTask task, CancellationToken ct = default)
    {
        await dbContext.PaymentTasks.AddAsync(task, ct);
        // SaveChangesAsync is intentionally not called here — the caller's
        // SaveChanges commits payment + task in one transaction.
    }
}
