using System.Text.Json;

namespace DF.PaymentService.Domain.Entities;

public class OutboxMessage
{
    public Guid Id { get; set; }
    public string Type { get; set; } = default!;
    public string Content { get; set; } = default!;
    public DateTime OccurredOn { get; set; }

    // Нові поля для надійності
    public DateTime? ProcessedOn { get; set; }
    public string? Error { get; set; }
    public int RetryCount { get; set; }
    public DateTime? NextAttempt { get; set; }
    public bool Poisoned { get; set; } // якщо не хочемо переносити в окрему таблицю

    // Корисно для кореляції
    public string? CorrelationId { get; set; }

    public static OutboxMessage Create(object domainEvent, string? correlationId = null)
    {
        return new OutboxMessage
        {
            Id = Guid.NewGuid(),
            Type = domainEvent.GetType().Name,
            Content = JsonSerializer.Serialize(domainEvent),
            OccurredOn = DateTime.UtcNow,
            RetryCount = 0,
            NextAttempt = DateTime.UtcNow,
            Poisoned = false,
            CorrelationId = correlationId
        };
    }
}