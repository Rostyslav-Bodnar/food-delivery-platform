using DF.PaymentService.Application.Commands;
using DF.PaymentService.Application.Repositories.Interfaces;
using DF.PaymentService.Application.Services.Interfaces;
using DF.PaymentService.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace DF.PaymentService.Application.CommandHandlers;

public class CancelPaymentCommandHandler(
    IPaymentRepository repo,
    IStripeService stripe,
    ILogger<CancelPaymentCommandHandler> logger)
{
    public async Task Handle(CancelPaymentCommand cmd, CancellationToken ct = default)
    {
        var payment = await repo.GetByIdAsync(cmd.PaymentId, ct);
        if (payment is null) return; // або кинути NotFoundException

        // Дозволено відміняти доки платіж не фіналізований (Succeeded/Refunded)
        if (payment.Status is PaymentStatus.Succeeded or PaymentStatus.Refunded)
            throw new InvalidOperationException("Cannot cancel finalized payment.");

        // Для Online: якщо Pending/RequiresAction, намагаємось скасувати PI у Stripe
        if (payment.Method == PaymentMethod.Online &&
            (payment.Status == PaymentStatus.Pending || payment.Status == PaymentStatus.RequiresAction))
        {
            try
            {
                await stripe.CancelPaymentIntentAsync(payment, ct);
            }
            catch (Exception ex)
            {
                // Не блокуємо бізнес-скасування, але лог дамо
                logger.LogWarning(ex, "Stripe cancel PI failed. PaymentId={PaymentId}, PI={PI}",
                    payment.Id, payment.StripePaymentIntentId);
            }
        }

        payment.CancelWithReason(cmd.Reason ?? "user_cancel");
        await repo.SaveChangesAsync(ct);
    }
}