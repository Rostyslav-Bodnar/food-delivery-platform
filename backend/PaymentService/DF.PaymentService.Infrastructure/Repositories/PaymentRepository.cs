using DF.PaymentService.Application.Repositories.Interfaces;
using DF.PaymentService.Domain.Entities;
using DF.PaymentService.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace DF.PaymentService.Infrastructure.Repositories;

public class PaymentRepository(AppDbContext dbContext) : IPaymentRepository
{
    public async Task AddAsync(Payment payment, CancellationToken ct = default)
    {
        // Aggregate root — додаємо як є (EF відтрекує owned властивості)
        await dbContext.Payments.AddAsync(payment, ct);
    }

    public async Task<Payment?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        // Трекінг увімкнено — ми часто змінюємо стан і робимо SaveChanges.
        // Якщо інколи потрібне читання без змін — можна зробити окремий метод з AsNoTracking.
        return await dbContext.Payments
            .FirstOrDefaultAsync(p => p.Id == id, ct);
    }

    public async Task<Payment?> GetByOrderIdAsync(Guid orderId, CancellationToken ct = default)
    {
        return await dbContext.Payments
            .FirstOrDefaultAsync(p => p.OrderId == orderId, ct);
    }

    public async Task<Payment?> GetByStripePaymentIntentIdAsync(string paymentIntentId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(paymentIntentId))
            throw new ArgumentException("Stripe Payment Intent Id is required.", nameof(paymentIntentId));

        return await dbContext.Payments
            .FirstOrDefaultAsync(p => p.StripePaymentIntentId == paymentIntentId, ct);
    }

    public async Task SaveChangesAsync(CancellationToken ct = default)
    {
        await dbContext.SaveChangesAsync(ct);
    }
}