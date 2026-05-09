namespace DF.PaymentService.Application.Commands;

public sealed record CancelPaymentCommand(Guid PaymentId, string? Reason = null);
