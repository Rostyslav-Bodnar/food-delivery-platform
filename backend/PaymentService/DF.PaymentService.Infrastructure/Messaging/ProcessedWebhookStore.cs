using DF.PaymentService.Application.Common.Interfaces;
using DF.PaymentService.Domain.Entities;
using DF.PaymentService.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Npgsql;

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
    /// Postgres-specific unique-violation check via SQLSTATE 23505 (rather than a fragile
    /// string match on the error message, which can vary across provider versions and locales).
    /// </summary>
    private static bool IsUniqueViolation(DbUpdateException ex)
    {
        if (ex.InnerException is PostgresException pgEx)
            return pgEx.SqlState == PostgresErrorCodes.UniqueViolation; // "23505"

        // Defensive fallback for non-Postgres providers — should not trigger in prod.
        var msg = ex.InnerException?.Message ?? ex.Message;
        return msg.Contains("23505", StringComparison.Ordinal)
               || msg.Contains("unique_violation", StringComparison.OrdinalIgnoreCase);
    }
}