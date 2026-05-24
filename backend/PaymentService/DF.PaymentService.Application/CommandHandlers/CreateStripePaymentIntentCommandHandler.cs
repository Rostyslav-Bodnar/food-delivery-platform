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

        // Destination-charge path when the business is onboarded with Stripe Connect.
        // Falls back to a plain platform charge when DestinationStripeAccountId is null.
        var result = payment.FundsFlow == FundsFlow.Destination
                     && !string.IsNullOrWhiteSpace(payment.DestinationStripeAccountId)
            ? await stripe.CreateDestinationPaymentIntentAsync(
                payment,
                payment.DestinationStripeAccountId!,
                ct: ct)
            : await stripe.CreatePaymentIntentAsync(payment, ct);

        payment.SetStripeSecrets(result.PaymentIntentId, result.ClientSecret);

        // Виставити TTL на підтвердження (наприклад, 15 хв)
        payment.SetExpiration(DateTime.UtcNow.AddMinutes(15));

        await repo.SaveChangesAsync(ct);
    }
}