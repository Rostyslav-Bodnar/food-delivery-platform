using DF.PaymentService.Application.Common.Interfaces;
using DF.PaymentService.Domain.Entities;
using DF.PaymentService.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DF.PaymentService.Infrastructure.Messaging;

public class ProcessedMessageStore(AppDbContext db, ILogger<ProcessedMessageStore> logger)
    : IProcessedMessageStore
{
    public async Task<bool> ExistsAsync(string messageId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(messageId))
            throw new ArgumentException("MessageId is required.", nameof(messageId));

        // Швидка перевірка існування
        return await db.ProcessedMessages
            .AsNoTracking()
            .AnyAsync(x => x.MessageId == messageId, ct);
    }

    public async Task MarkProcessedAsync(string messageId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(messageId))
            throw new ArgumentException("MessageId is required.", nameof(messageId));

        // Оптимістична вставка (з урахуванням гонок): намагаємось вставити
        // Якщо вже є — отримаємо unique violation і просто ігноруємо
        var entity = new ProcessedMessage
        {
            MessageId = messageId,
            ReceivedAtUtc = DateTime.UtcNow
        };

        db.ProcessedMessages.Add(entity);

        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex)
        {
            // Якщо це порушення унікальності — ок, інший воркер вже записав
            if (IsUniqueViolation(ex))
            {
                logger.LogDebug("ProcessedMessage duplicate insert ignored (MessageId={MessageId})", messageId);
                db.Entry(entity).State = EntityState.Detached;
                return;
            }

            // Інакше — проброс
            throw;
        }
    }

    /// <summary>
    /// Грубе визначення unique violation для популярних провайдерів (SQL Server / PostgreSQL / SQLite).
    /// У production краще робити провайдер‑специфічну перевірку за кодами помилок.
    /// </summary>
    private static bool IsUniqueViolation(DbUpdateException ex)
    {
        var msg = ex.InnerException?.Message ?? ex.Message;
        // SQL Server, PostgreSQL, SQLite зазвичай містять у тексті "UNIQUE" або "duplicate"
        return msg.Contains("UNIQUE", StringComparison.OrdinalIgnoreCase)
               || msg.Contains("duplicate", StringComparison.OrdinalIgnoreCase)
               || msg.Contains("constraint", StringComparison.OrdinalIgnoreCase);
    }
}