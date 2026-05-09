namespace DF.PaymentService.Infrastructure.BackgroundJobs;

public sealed class PaymentTimeoutOptions
{
    public int BatchSize { get; init; } = 100;
    public TimeSpan PollingInterval { get; init; } = TimeSpan.FromMinutes(1);
    public TimeSpan SafetyWindow { get; init; } = TimeSpan.FromSeconds(10);
    public bool Enable { get; init; } = true;

    /// <summary>
    /// Якщо true — для Online платежів спробувати скасувати PaymentIntent у Stripe.
    /// </summary>
    public bool CancelStripePiOnTimeout { get; init; } = false;
}