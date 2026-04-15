namespace DF.PaymentService.Infrastructure.BackgroundJobs;

public sealed class CourierPayoutOptions
{
    public bool Enable { get; init; } = true;
    public int PollingIntervalSeconds { get; init; } = 300;
    public int HoldPeriodHours { get; init; } = 24;
    public decimal MinimumPayoutAmount { get; init; } = 1m;
    public string DefaultCurrency { get; init; } = "USD";

    public TimeSpan PollingInterval => TimeSpan.FromSeconds(Math.Max(1, PollingIntervalSeconds));
    public TimeSpan HoldPeriod => TimeSpan.FromHours(Math.Max(0, HoldPeriodHours));
}
