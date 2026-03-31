namespace DF.UserService.Domain.Entities;

public class ProcessedWebhook
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string WebhookId { get; set; } = default!;
    public DateTime ProcessedAtUtc { get; set; } = DateTime.UtcNow;
}
