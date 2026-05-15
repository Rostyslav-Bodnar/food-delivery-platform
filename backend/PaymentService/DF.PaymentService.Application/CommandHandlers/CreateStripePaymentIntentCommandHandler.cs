using DF.PaymentService.Application.Commands;
using DF.PaymentService.Application.Repositories.Interfaces;
using DF.PaymentService.Application.Services.Interfaces;
using DF.PaymentService.Domain.Entities;

namespace DF.PaymentService.Application.CommandHandlers;

public class CreateStripePaymentIntentCommandHandler(IPaymentRepository repo, IStripeService stripe)
{
    public async Task Handle(CreateStripePaymentIntentCommand cmd, CancellationToken ct = default)
    {
        var payment = await repo.GetByIdAsync(cmd.PaymentId, ct);
        if (payment is null) return;

        if (payment.Method != PaymentMethod.Online)
            throw new InvalidOperationException("PaymentIntent is applicable only for Online payments.");

        // Якщо PI вже створено — нічого не робимо (ідемпотентність)
        if (!string.IsNullOrWhiteSpace(payment.StripePaymentIntentId))
            return;

        var result = await stripe.CreatePaymentIntentAsync(payment, ct);
        payment.SetStripeSecrets(result.PaymentIntentId, result.ClientSecret);

        // Виставити TTL на підтвердження (наприклад, 15 хв)
        payment.SetExpiration(DateTime.UtcNow.AddMinutes(15));

        await repo.SaveChangesAsync(ct);
    }
}