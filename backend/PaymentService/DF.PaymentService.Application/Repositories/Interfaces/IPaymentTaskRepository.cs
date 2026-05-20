using DF.PaymentService.Domain.Entities;

namespace DF.PaymentService.Application.Repositories.Interfaces;

public interface IPaymentTaskRepository
{
    Task AddAsync(PaymentTask task, CancellationToken ct = default);
}
