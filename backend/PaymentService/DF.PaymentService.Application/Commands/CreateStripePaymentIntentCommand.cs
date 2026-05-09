namespace DF.PaymentService.Application.Commands;

public sealed record CreateStripePaymentIntentCommand(Guid PaymentId);
