namespace DF.PaymentService.Domain.Entities;

public class ProcessedWebhook
{
    public string EventId { get; set; } = default!;
    public DateTime ReceivedAtUtc { get; set; }
}
