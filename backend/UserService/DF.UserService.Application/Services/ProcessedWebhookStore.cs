using DF.UserService.Application.Services.Interfaces;
using DF.UserService.Domain.Entities;
using DF.UserService.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace DF.UserService.Application.Services;

public class ProcessedWebhookStore : IProcessedWebhookStore
{
    private readonly AppDbContext _db;

    public ProcessedWebhookStore(AppDbContext db)
    {
        _db = db;
    }

    public async Task<bool> ExistsAsync(string webhookId, CancellationToken ct)
    {
        return await _db.ProcessedWebhooks
            .AnyAsync(x => x.WebhookId == webhookId, ct);
    }

    public async Task MarkProcessedAsync(string webhookId, CancellationToken ct)
    {
        _db.ProcessedWebhooks.Add(new ProcessedWebhook
        {
            WebhookId = webhookId
        });

        await _db.SaveChangesAsync(ct);
    }
}