namespace DF.PaymentService.Application.Commands;

public sealed record RefundPaymentCommand(Guid PaymentId, decimal? Amount);
