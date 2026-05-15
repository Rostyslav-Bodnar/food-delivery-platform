namespace DF.PaymentService.Application.Common.Interfaces;

public interface IProcessedMessageStore
{
    Task<bool> ExistsAsync(string messageId, CancellationToken ct = default);
    Task MarkProcessedAsync(string messageId, CancellationToken ct = default);
}