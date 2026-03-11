using DF.PaymentService.Application.Commands;
using DF.PaymentService.Application.Repositories.Interfaces;
using DF.PaymentService.Domain.Entities;

namespace DF.PaymentService.Application.CommandHandlers;

public class CreatePaymentCommandHandler(IPaymentRepository repository)
{
    public async Task Handle(
        CreatePaymentCommand command,
        CancellationToken cancellationToken = default)
    {
        // 🔐 Idempotency check
        var existingPayment = await repository
            .GetByOrderIdAsync(command.OrderId, cancellationToken);

        if (existingPayment is not null)
            return; // already processed

        var payment = Payment.CreatePayment(
            command.OrderId,
            new Money(command.Amount, command.Currency),
            command.Method);

        await repository.AddAsync(payment, cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);
    }
}