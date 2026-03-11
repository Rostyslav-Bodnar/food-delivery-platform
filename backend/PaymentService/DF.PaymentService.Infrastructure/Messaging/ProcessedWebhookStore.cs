using DF.PaymentService.Application.Common.Interfaces;
using DF.PaymentService.Domain.Entities;
using DF.PaymentService.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DF.PaymentService.Infrastructure.Messaging;

/// <summary>
/// Durable store для ідемпотентності Stripe webhooks за EventId.
/// Гарантує, що кожен webhook подія буде оброблена рівно один раз.
/// </summary>
public class ProcessedWebhookStore(AppDbContext db, ILogger<ProcessedWebhookStore> logger)
    : IProcessedWebhookStore
{
    public async Task<bool> ExistsAsync(string eventId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(eventId))
            throw new ArgumentException("EventId is required.", nameof(eventId));

        return await db.ProcessedWebhooks
            .AsNoTracking()
            .AnyAsync(x => x.EventId == eventId, ct);
    }

    public async Task MarkProcessedAsync(string eventId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(eventId))
            throw new ArgumentException("EventId is required.", nameof(eventId));

        var entity = new ProcessedWebhook
        {
            EventId = eventId,
            ReceivedAtUtc = DateTime.UtcNow
        };

        db.ProcessedWebhooks.Add(entity);

        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex)
        {
            // Якщо інший інстанс/потік уже записав — ігноруємо дубль (ідемпотентність)
            if (IsUniqueViolation(ex))
            {
                logger.LogDebug("Duplicate webhook EventId ignored (EventId={EventId})", eventId);
                db.Entry(entity).State = EntityState.Detached;
                return;
            }

            throw;
        }
    }

    /// <summary>
    /// Грубе визначення порушення унікальності для різних провайдерів (SQL Server/PostgreSQL/SQLite).
    /// У prod бажано перевіряти коди помилок провайдера.
    /// </summary>
    private static bool IsUniqueViolation(DbUpdateException ex)
    {
        var msg = ex.InnerException?.Message ?? ex.Message;
        return msg.Contains("UNIQUE", StringComparison.OrdinalIgnoreCase)
               || msg.Contains("duplicate", StringComparison.OrdinalIgnoreCase)
               || msg.Contains("constraint", StringComparison.OrdinalIgnoreCase);
    }
}