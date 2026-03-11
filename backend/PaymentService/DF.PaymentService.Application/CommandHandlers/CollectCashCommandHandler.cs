using DF.PaymentService.Application.Commands;
using DF.PaymentService.Application.Repositories.Interfaces;
using DF.PaymentService.Domain.Entities;

namespace DF.PaymentService.Application.CommandHandlers;

public class CollectCashCommandHandler(IPaymentRepository repo)
{
    public async Task Handle(CollectCashCommand cmd, CancellationToken ct = default)
    {
        var payment = await repo.GetByIdAsync(cmd.PaymentId, ct);
        if (payment is null) return;

        if (payment.Method != PaymentMethod.CashOnDelivery)
            throw new InvalidOperationException("Collect cash is applicable only for CashOnDelivery.");

        if (payment.Status != PaymentStatus.AwaitingCashCollection)
            throw new InvalidOperationException("Cash can be collected only from AwaitingCashCollection.");

        payment.MarkSucceeded();
        await repo.SaveChangesAsync(ct);
    }
}