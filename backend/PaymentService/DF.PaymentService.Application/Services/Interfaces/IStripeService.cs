using DF.PaymentService.Contracts;
using DF.PaymentService.Domain.Entities;
using Stripe;

namespace DF.PaymentService.Application.Services.Interfaces;

public interface IStripeService
{
    Task<StripePaymentIntentResult> CreatePaymentIntentAsync(Payment payment, CancellationToken ct = default);

    // ✅ Виправлений підпис: приймаємо Payment, а не Guid/amountMinor
    Task<string> RefundAsync(Payment payment, decimal? amount = null, CancellationToken ct = default);

    // ✅ Нове: для кейсів 2/6 (user cancel/timeout)
    Task CancelPaymentIntentAsync(Payment payment, CancellationToken ct = default);

    // 🔄 Опційно: якщо робите confirm з бекенду (authorize/capture або контроль RA)
    Task<PaymentIntent> ConfirmPaymentIntentAsync(Payment payment,
        PaymentIntentConfirmOptions? options = null,
        CancellationToken ct = default);
}
