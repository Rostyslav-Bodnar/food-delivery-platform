using System.Text.Json;
using DF.OrderService.Domain.Entities;
using DF.OrderService.Infrastructure.Data;

namespace DF.OrderService.Application.Messaging.Publishers;

/// <summary>
/// Writes domain events to the Outbox table. The caller is responsible for the
/// SaveChanges that commits both the business write and the outbox row in one tx.
/// </summary>
public sealed class OutboxWriter(AppDbContext db)
{
    public Task EnqueueAsync<T>(T @event) where T : class
    {
        var msg = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            EventType = typeof(T).Name,
            Payload = JsonSerializer.Serialize(@event),
            OccurredAtUtc = DateTime.UtcNow
        };
        return db.OutboxMessages.AddAsync(msg).AsTask();
    }
}
