namespace DF.OrderService.Application.Options;

/// <summary>
/// Configuration for <c>OrderPaymentTimeoutWorker</c>. Defaults match the
/// 15-minute "pay or lose the order" window common across food-delivery
/// platforms.
/// </summary>
public sealed class OrderPaymentTimeoutOptions
{
    public bool Enabled { get; init; } = true;

    /// <summary>How often the worker checks for stale unpaid orders.</summary>
    public TimeSpan PollingInterval { get; init; } = TimeSpan.FromMinutes(1);

    /// <summary>
    /// How long an Online order can sit unpaid before it's auto-canceled.
    /// Measured from <c>Order.OrderDate</c>.
    /// </summary>
    public TimeSpan PaymentDeadline { get; init; } = TimeSpan.FromMinutes(15);

    /// <summary>Cap per poll so we don't lock up the DB on a backlog.</summary>
    public int BatchSize { get; init; } = 50;
}
