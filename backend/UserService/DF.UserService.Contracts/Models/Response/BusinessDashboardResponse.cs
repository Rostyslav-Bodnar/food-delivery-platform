namespace DF.UserService.Contracts.Models.Response;

/// <summary>
/// Aggregate financial dashboard for a business, computed live from the
/// business's Stripe Connect account. All monetary fields are in the
/// account's settlement currency (typically USD) as decimals already
/// converted from Stripe's minor-unit integers.
/// </summary>
public sealed record BusinessDashboardResponse(
    BusinessDashboardSummary Summary,
    IReadOnlyList<DashboardTimePoint> TimeSeries,
    IReadOnlyList<DashboardBreakdownItem> OutcomeBreakdown,
    IReadOnlyList<DashboardPayout> Payouts,
    BusinessBalanceSnapshot Balance,
    DashboardWindow Window,
    string Currency
);

public sealed record BusinessDashboardSummary(
    decimal GrossSales,
    decimal Refunds,
    decimal PlatformFees,
    decimal StripeProcessingFees,
    decimal NetIncome,
    decimal PaidOut,
    int Orders,
    int RefundedOrders
);

public sealed record DashboardTimePoint(
    DateTime Date,        // UTC midnight of the day
    decimal Gross,
    decimal Refunds,
    decimal Fees,
    decimal Net
);

public sealed record DashboardBreakdownItem(
    string Category,      // "Net income" | "Platform fee" | "Stripe fees" | "Refunds"
    decimal Amount
);

public sealed record DashboardPayout(
    string Id,
    DateTime CreatedAtUtc,
    DateTime? ArrivalUtc,
    DateTime? PaidAtUtc,
    decimal Amount,
    string Currency,
    string Status,         // pending/in_transit/paid/failed/canceled
    string? FailureMessage
);

public sealed record BusinessBalanceSnapshot(
    decimal Available,
    decimal Pending,
    string Currency
);

public sealed record DashboardWindow(
    DateTime FromUtc,
    DateTime ToUtc
);
