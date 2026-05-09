namespace DF.PaymentService.Application.Services.Interfaces;

public interface ICourierPayoutService
{
    Task RegisterDeliveredOrderAsync(
        Guid orderId,
        Guid courierId,
        decimal amount,
        string currency,
        DateTime deliveredAtUtc,
        CancellationToken ct = default);

    Task ReleaseHeldEarningAsync(Guid orderId, CancellationToken ct = default);

    Task VoidOrderEarningAsync(Guid orderId, CancellationToken ct = default);

    Task UpsertCourierStripeAccountAsync(Guid courierId, string stripeAccountId, CancellationToken ct = default);

    Task<CourierBalanceSnapshot> GetBalanceAsync(Guid courierId, CancellationToken ct = default);

    Task<int> ProcessDuePayoutsAsync(CancellationToken ct = default);
}

public sealed record CourierBalanceSnapshot(
    Guid CourierId,
    decimal PendingAmount,
    decimal AvailableAmount,
    string Currency,
    string? StripeAccountId,
    bool PayoutsEnabled
);
