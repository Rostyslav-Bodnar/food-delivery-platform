namespace DF.OrderService.Domain.Entities;

public class OutboxMessage
{
    public Guid Id { get; set; }
    public required string EventType { get; set; }
    public required string Payload { get; set; }
    public DateTime OccurredAtUtc { get; set; }
    public DateTime? DispatchedAtUtc { get; set; }
    public int RetryCount { get; set; }
    public string? LastError { get; set; }
}
