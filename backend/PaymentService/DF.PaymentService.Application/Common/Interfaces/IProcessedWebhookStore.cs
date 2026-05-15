namespace DF.PaymentService.Application.Common.Interfaces;

public interface IProcessedWebhookStore
{
    Task<bool> ExistsAsync(string eventId, CancellationToken ct = default);
    Task MarkProcessedAsync(string eventId, CancellationToken ct = default);
}
