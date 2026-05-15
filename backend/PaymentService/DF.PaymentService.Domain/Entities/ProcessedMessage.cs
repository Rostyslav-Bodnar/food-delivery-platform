namespace DF.PaymentService.Domain.Entities;

public class ProcessedMessage
{
    public string MessageId { get; set; } = default!;
    public DateTime ReceivedAtUtc { get; set; }
}