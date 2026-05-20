using DF.PaymentService.Application.Commands;
using DF.PaymentService.Application.Repositories.Interfaces;
using DF.PaymentService.Application.Services.Interfaces;
using DF.PaymentService.Domain.Entities;

namespace DF.PaymentService.Application.CommandHandlers;

public class RefundPaymentCommandHandler(IPaymentRepository repo, IStripeService stripe)
{
    public async Task Handle(RefundPaymentCommand cmd, CancellationToken ct = default)
    {
        var payment = await repo.GetByIdAsync(cmd.PaymentId, ct);
        if (payment is null) return;

        // Рефанд дозволено лише зі Succeeded
        if (payment.Status != PaymentStatus.Succeeded)
            throw new InvalidOperationException("Only succeeded payment can be refunded.");

        // Для Online — викликаємо Stripe; домен оновиться по вебхуку (refund.succeeded / charge.refunded)
        if (payment.Method == PaymentMethod.Online)
        {
            if (payment.FundsFlow == FundsFlow.Destination)
            {
                await stripe.RefundDestinationAsync(payment, cmd.Amount, cmd.IdempotencyKey, ct);
                return;
            }

            // Інакше — звичайний (SCT/standard) рефанд без reverse_transfer
            await stripe.RefundAsync(payment, cmd.Amount, cmd.IdempotencyKey, ct);
            return;
        }


        // Для CoD — рефанд виконується поза Stripe: змінюємо домен напряму
        if (cmd.Amount.HasValue)
        {
            var money = new Money(cmd.Amount.Value, payment.Amount.Currency);
            payment.ApplyPartialRefund(money);
        }
        else
        {
            payment.ApplyFullRefund();
        }

        await repo.SaveChangesAsync(ct);
    }
}