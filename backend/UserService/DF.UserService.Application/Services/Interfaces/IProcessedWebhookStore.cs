namespace DF.UserService.Application.Services.Interfaces;

public interface IProcessedWebhookStore
{
    Task<bool> ExistsAsync(string webhookId, CancellationToken ct);
    Task MarkProcessedAsync(string webhookId, CancellationToken ct);
}
