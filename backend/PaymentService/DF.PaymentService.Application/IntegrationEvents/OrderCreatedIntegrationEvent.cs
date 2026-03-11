namespace DF.PaymentService.Application.IntegrationEvents;

public class OrderCreatedIntegrationEvent
{
    public Guid OrderId { get; init; }
    public decimal Amount { get; init; }
    public string Currency { get; init; } = default!;
    public string PaymentMethod { get; init; } = default!;
}