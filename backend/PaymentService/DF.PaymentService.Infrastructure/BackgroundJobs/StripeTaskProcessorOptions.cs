namespace DF.PaymentService.Infrastructure.BackgroundJobs;

public sealed class StripeTaskProcessorOptions
{
    public int BatchSize { get; init; } = 50;
    public TimeSpan PollingInterval { get; init; } = TimeSpan.FromSeconds(5);
    public int MaxRetries { get; init; } = 5;
    public TimeSpan BaseDelay { get; init; } = TimeSpan.FromSeconds(2);
    public TimeSpan MaxDelay { get; init; } = TimeSpan.FromMinutes(2);
    public TimeSpan PaymentExpiration { get; init; } = TimeSpan.FromMinutes(15);
    public bool Enable { get; init; } = true;
}