using DF.PaymentService.Application.Commands;
using DF.PaymentService.Application.Repositories.Interfaces;
using DF.PaymentService.Domain.Entities;

namespace DF.PaymentService.Application.CommandHandlers;

public class CreatePaymentCommandHandler(
    IPaymentRepository repository,
    IPaymentTaskRepository taskRepository)
{
    public async Task Handle(
        CreatePaymentCommand command,
        CancellationToken cancellationToken = default)
    {
        // 🔐 Idempotency check — already-processed OrderCreatedEvent is a no-op.
        var existingPayment = await repository
            .GetByOrderIdAsync(command.OrderId, cancellationToken);

        if (existingPayment is not null)
            return;

        var payment = Payment.CreatePayment(
            command.OrderId,
            new Money(command.Amount, command.Currency),
            command.Method);

        // Online + a known connected account → destination charge. When the
        // business hasn't onboarded yet, the field arrives empty and we fall
        // back to a platform charge (kept for forward-compat with old data).
        if (command.Method == PaymentMethod.Online
            && !string.IsNullOrWhiteSpace(command.DestinationStripeAccountId))
        {
            payment.SetDestinationAccount(command.DestinationStripeAccountId);
        }

        await repository.AddAsync(payment, cancellationToken);

        // For Online payments, stage a PaymentTask in the same transaction so the
        // StripeTaskProcessor (with its IsNullOrWhiteSpace(StripePaymentIntentId)
        // guard) is the single place that actually calls Stripe — no race with the
        // consumer side, no duplicate PIs on event redelivery.
        if (command.Method == PaymentMethod.Online)
        {
            await taskRepository.AddAsync(
                PaymentTask.Create(payment.Id, PaymentTaskType.CreateStripePaymentIntent),
                cancellationToken);
        }

        await repository.SaveChangesAsync(cancellationToken);
    }
}
