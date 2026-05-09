using DF.PaymentService.Domain.Entities;

namespace DF.PaymentService.Application.Commands;

public class CreatePaymentCommand(
    Guid orderId,
    decimal amount,
    string currency,
    PaymentMethod method)
{
    public Guid OrderId { get; } = orderId;
    public decimal Amount { get; } = amount;
    public string Currency { get; } = currency;
    public PaymentMethod Method { get; } = method;
}