using DF.PaymentService.Domain.Entities;

namespace DF.PaymentService.Application.Repositories.Interfaces;

public interface IPaymentRepository
{
    Task<Payment?> GetByIdAsync(Guid id, CancellationToken ct = default);

    Task<Payment?> GetByOrderIdAsync(Guid orderId, CancellationToken ct = default);

    // ✅ ДОДАНО: для обробки webhooks (refund/dispute/charge.refunded)
    Task<Payment?> GetByStripePaymentIntentIdAsync(string paymentIntentId, CancellationToken ct = default);

    Task AddAsync(Payment payment, CancellationToken ct = default);

    Task SaveChangesAsync(CancellationToken ct = default);
}