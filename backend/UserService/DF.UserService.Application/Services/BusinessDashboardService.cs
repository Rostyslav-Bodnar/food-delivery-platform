using DF.UserService.Application.Repositories.Interfaces;
using DF.UserService.Application.Services.Interfaces;
using DF.UserService.Contracts.Models.DTO;
using DF.UserService.Contracts.Models.Response;
using DF.UserService.Domain.Entities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Stripe;

namespace DF.UserService.Application.Services;

/// <summary>
/// Builds the per-business financial dashboard by querying Stripe Connect
/// directly. We don't persist anything new — the dashboard view is live.
/// </summary>
public sealed class BusinessDashboardService : IBusinessDashboardService
{
    private const decimal DefaultCurrencyScale = 100m; // USD/EUR/etc
    private const string DefaultCurrency = "usd";

    private readonly StripeClient _client;
    private readonly IServiceProvider _services;
    private readonly ILogger<BusinessDashboardService> _logger;

    public BusinessDashboardService(
        IOptions<StripeOptions> options,
        IServiceProvider services,
        ILogger<BusinessDashboardService> logger)
    {
        if (string.IsNullOrWhiteSpace(options.Value.SecretKey))
            throw new InvalidOperationException("Stripe SecretKey is not configured.");

        _client = new StripeClient(options.Value.SecretKey);
        _services = services;
        _logger = logger;
    }

    public async Task<BusinessDashboardResponse?> GetDashboardAsync(
        Guid businessId,
        DateTime fromUtc,
        DateTime toUtc,
        CancellationToken ct = default)
    {
        if (fromUtc >= toUtc)
            throw new ArgumentException("`from` must be earlier than `to`.");

        // Cap window at 366 days so a buggy caller can't hammer Stripe.
        if ((toUtc - fromUtc).TotalDays > 366)
            throw new ArgumentException("Window must be 366 days or less.");

        // Resolve Stripe account ID — service is singleton, repo is scoped, so
        // open a fresh scope per call (matches StripeConnectService DI pattern).
        string? stripeAccountId;
        using (var scope = _services.CreateScope())
        {
            var accounts = scope.ServiceProvider.GetRequiredService<IAccountRepository>();
            var account = await accounts.Get(businessId);
            if (account is not BusinessAccount business) return null;
            stripeAccountId = business.StripeAccountId;
        }

        if (string.IsNullOrWhiteSpace(stripeAccountId))
            return null;

        var requestOptions = new RequestOptions { StripeAccount = stripeAccountId };
        var balanceTxs = await FetchBalanceTransactionsAsync(fromUtc, toUtc, requestOptions, ct);
        var payouts = await FetchPayoutsAsync(fromUtc, toUtc, requestOptions, ct);
        var balance = await FetchBalanceAsync(requestOptions, ct);

        var currency = ResolveCurrency(balanceTxs, payouts, balance);

        var summary = ComputeSummary(balanceTxs);
        var series = ComputeTimeSeries(balanceTxs, fromUtc, toUtc);
        var breakdown = ComputeBreakdown(summary);

        return new BusinessDashboardResponse(
            Summary: summary,
            TimeSeries: series,
            OutcomeBreakdown: breakdown,
            Payouts: MapPayouts(payouts),
            Balance: balance,
            Window: new DashboardWindow(fromUtc, toUtc),
            Currency: currency
        );
    }

    // -----------------------------------------------------------------
    // Stripe fetches
    // -----------------------------------------------------------------

    private async Task<List<BalanceTransaction>> FetchBalanceTransactionsAsync(
        DateTime fromUtc,
        DateTime toUtc,
        RequestOptions requestOptions,
        CancellationToken ct)
    {
        var service = new BalanceTransactionService(_client);
        var list = new List<BalanceTransaction>();

        var listOptions = new BalanceTransactionListOptions
        {
            Created = new DateRangeOptions
            {
                GreaterThanOrEqual = fromUtc,
                LessThanOrEqual = toUtc
            },
            Limit = 100
        };

        await foreach (var tx in service
                           .ListAutoPagingAsync(listOptions, requestOptions, ct)
                           .WithCancellation(ct))
        {
            list.Add(tx);
        }

        return list;
    }

    private async Task<List<Payout>> FetchPayoutsAsync(
        DateTime fromUtc,
        DateTime toUtc,
        RequestOptions requestOptions,
        CancellationToken ct)
    {
        var service = new PayoutService(_client);
        var list = new List<Payout>();

        var listOptions = new PayoutListOptions
        {
            Created = new DateRangeOptions
            {
                GreaterThanOrEqual = fromUtc,
                LessThanOrEqual = toUtc
            },
            Limit = 100
        };

        await foreach (var p in service
                           .ListAutoPagingAsync(listOptions, requestOptions, ct)
                           .WithCancellation(ct))
        {
            list.Add(p);
        }

        return list;
    }

    private async Task<BusinessBalanceSnapshot> FetchBalanceAsync(
        RequestOptions requestOptions,
        CancellationToken ct)
    {
        try
        {
            var service = new BalanceService(_client);
            var balance = await service.GetAsync(null, requestOptions, ct);

            // Take the largest available balance entry (multi-currency accounts
            // are rare in this app, but pick the principal currency rather
            // than blowing up if both USD + EUR exist).
            var primaryAvailable = balance.Available?
                .OrderByDescending(b => b.Amount)
                .FirstOrDefault();
            var primaryPending = balance.Pending?
                .OrderByDescending(b => b.Amount)
                .FirstOrDefault();

            var currency = primaryAvailable?.Currency ?? primaryPending?.Currency ?? DefaultCurrency;

            return new BusinessBalanceSnapshot(
                Available: FromMinor(primaryAvailable?.Amount ?? 0, currency),
                Pending: FromMinor(primaryPending?.Amount ?? 0, currency),
                Currency: currency.ToUpperInvariant()
            );
        }
        catch (StripeException ex)
        {
            // Don't fail the whole dashboard if Balance can't be fetched —
            // it's a "nice to have" KPI, not the headline number.
            _logger.LogWarning(ex, "Failed to fetch Stripe balance");
            return new BusinessBalanceSnapshot(0, 0, DefaultCurrency.ToUpperInvariant());
        }
    }

    // -----------------------------------------------------------------
    // Aggregations
    // -----------------------------------------------------------------

    /// <summary>
    /// Sum balance transactions into the KPI summary. Sign conventions:
    /// `payment`/`charge` amounts are positive; `refund`/`application_fee`/
    /// `payout` are negative on the connected account. We flip to positive
    /// numbers for display.
    /// </summary>
    private static BusinessDashboardSummary ComputeSummary(IEnumerable<BalanceTransaction> txs)
    {
        long grossMinor = 0;
        long refundsMinor = 0;
        long platformFeesMinor = 0;
        long stripeFeesMinor = 0;
        long paidOutMinor = 0;
        int orders = 0;
        int refundedOrders = 0;
        string currency = DefaultCurrency;

        foreach (var tx in txs)
        {
            currency = tx.Currency ?? currency;
            switch (tx.Type)
            {
                case "payment":
                case "charge":
                    if (tx.Amount > 0)
                    {
                        grossMinor += tx.Amount;
                        // tx.Fee is Stripe's processing fee on this charge.
                        stripeFeesMinor += tx.Fee;
                        orders++;
                    }
                    break;

                case "payment_refund":
                case "refund":
                case "payment_failure_refund":
                    if (tx.Amount < 0)
                    {
                        refundsMinor += -tx.Amount;
                        refundedOrders++;
                    }
                    break;

                case "application_fee":
                    if (tx.Amount < 0) platformFeesMinor += -tx.Amount;
                    break;

                case "application_fee_refund":
                    // Platform fee was refunded back to the connected account.
                    if (tx.Amount > 0) platformFeesMinor -= tx.Amount;
                    break;

                case "payout":
                    if (tx.Amount < 0) paidOutMinor += -tx.Amount;
                    break;

                case "payout_cancel":
                case "payout_failure":
                    if (tx.Amount > 0) paidOutMinor -= tx.Amount;
                    break;
            }
        }

        var gross = FromMinor(grossMinor, currency);
        var refunds = FromMinor(refundsMinor, currency);
        var platformFees = FromMinor(platformFeesMinor, currency);
        var stripeFees = FromMinor(stripeFeesMinor, currency);
        var paidOut = FromMinor(paidOutMinor, currency);

        // Net = what actually settled to the business after fees + refunds.
        var net = gross - refunds - platformFees - stripeFees;

        return new BusinessDashboardSummary(
            GrossSales: gross,
            Refunds: refunds,
            PlatformFees: platformFees,
            StripeProcessingFees: stripeFees,
            NetIncome: net,
            PaidOut: paidOut,
            Orders: orders,
            RefundedOrders: refundedOrders
        );
    }

    private static List<DashboardTimePoint> ComputeTimeSeries(
        IEnumerable<BalanceTransaction> txs,
        DateTime fromUtc,
        DateTime toUtc)
    {
        // Pre-seed every day in the window so the chart never has gaps.
        var startDay = fromUtc.Date;
        var endDay = toUtc.Date;
        var buckets = new SortedDictionary<DateTime, (long gross, long refunds, long fees)>();
        for (var d = startDay; d <= endDay; d = d.AddDays(1))
        {
            buckets[d] = (0, 0, 0);
        }

        var currency = DefaultCurrency;

        foreach (var tx in txs)
        {
            currency = tx.Currency ?? currency;
            var day = tx.Created.Date;
            if (!buckets.TryGetValue(day, out var bucket)) continue;

            switch (tx.Type)
            {
                case "payment":
                case "charge":
                    if (tx.Amount > 0)
                    {
                        bucket.gross += tx.Amount;
                        bucket.fees += tx.Fee;
                    }
                    break;
                case "payment_refund":
                case "refund":
                case "payment_failure_refund":
                    if (tx.Amount < 0) bucket.refunds += -tx.Amount;
                    break;
                case "application_fee":
                    if (tx.Amount < 0) bucket.fees += -tx.Amount;
                    break;
                case "application_fee_refund":
                    if (tx.Amount > 0) bucket.fees -= tx.Amount;
                    break;
            }

            buckets[day] = bucket;
        }

        return buckets.Select(kvp =>
        {
            var (gross, refunds, fees) = kvp.Value;
            var grossDec = FromMinor(gross, currency);
            var refundsDec = FromMinor(refunds, currency);
            var feesDec = FromMinor(fees, currency);
            return new DashboardTimePoint(
                Date: kvp.Key,
                Gross: grossDec,
                Refunds: refundsDec,
                Fees: feesDec,
                Net: grossDec - refundsDec - feesDec
            );
        }).ToList();
    }

    private static List<DashboardBreakdownItem> ComputeBreakdown(BusinessDashboardSummary summary)
    {
        var items = new List<DashboardBreakdownItem>(4);
        if (summary.NetIncome > 0) items.Add(new("Net income", summary.NetIncome));
        if (summary.PlatformFees > 0) items.Add(new("Platform fee", summary.PlatformFees));
        if (summary.StripeProcessingFees > 0) items.Add(new("Stripe fees", summary.StripeProcessingFees));
        if (summary.Refunds > 0) items.Add(new("Refunds", summary.Refunds));
        return items;
    }

    private static List<DashboardPayout> MapPayouts(IEnumerable<Payout> payouts)
    {
        return payouts
            .OrderByDescending(p => p.Created)
            .Select(p => new DashboardPayout(
                Id: p.Id,
                CreatedAtUtc: p.Created,
                ArrivalUtc: p.ArrivalDate,
                PaidAtUtc: p.Status == "paid" ? p.ArrivalDate : null,
                Amount: FromMinor(p.Amount, p.Currency ?? DefaultCurrency),
                Currency: (p.Currency ?? DefaultCurrency).ToUpperInvariant(),
                Status: p.Status ?? "unknown",
                FailureMessage: p.FailureMessage
            ))
            .ToList();
    }

    // -----------------------------------------------------------------
    // Helpers
    // -----------------------------------------------------------------

    private static string ResolveCurrency(
        IEnumerable<BalanceTransaction> txs,
        IEnumerable<Payout> payouts,
        BusinessBalanceSnapshot balance)
    {
        var fromTx = txs.FirstOrDefault()?.Currency;
        var fromPayout = payouts.FirstOrDefault()?.Currency;
        var c = fromTx ?? fromPayout ?? balance.Currency.ToLowerInvariant();
        return string.IsNullOrWhiteSpace(c) ? DefaultCurrency.ToUpperInvariant() : c.ToUpperInvariant();
    }

    private static decimal FromMinor(long amount, string currency)
    {
        var scale = GetCurrencyScale(currency);
        return scale == 0
            ? amount
            : amount / (decimal)Math.Pow(10, scale);
    }

    private static int GetCurrencyScale(string currency) =>
        currency.ToUpperInvariant() switch
        {
            "JPY" or "KRW" => 0,
            _ => 2
        };
}
