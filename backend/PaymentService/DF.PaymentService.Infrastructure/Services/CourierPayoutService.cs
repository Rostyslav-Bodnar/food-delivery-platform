using DF.PaymentService.Application.Services.Interfaces;
using DF.PaymentService.Domain.Entities;
using DF.PaymentService.Infrastructure.BackgroundJobs;
using DF.PaymentService.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DF.PaymentService.Infrastructure.Services;

public class CourierPayoutService(
    AppDbContext db,
    IStripeService stripe,
    CourierPayoutOptions options,
    ILogger<CourierPayoutService> logger) : ICourierPayoutService
{
    public async Task RegisterDeliveredOrderAsync(
        Guid orderId,
        Guid courierId,
        decimal amount,
        string currency,
        DateTime deliveredAtUtc,
        CancellationToken ct = default)
    {
        if (amount <= 0m)
            return;

        var existing = await db.CourierEarnings
            .FirstOrDefaultAsync(x => x.OrderId == orderId, ct);

        if (existing is not null)
            return;

        var payment = await db.Payments
            .FirstOrDefaultAsync(x => x.OrderId == orderId, ct)
            ?? throw new InvalidOperationException($"Payment for order {orderId} was not found.");

        var normalizedCurrency = NormalizeCurrency(currency);
        var balance = await GetOrCreateBalanceAsync(courierId, normalizedCurrency, ct);
        var roundedAmount = decimal.Round(amount, 2, MidpointRounding.AwayFromZero);

        CourierEarning earning;
        if (payment.Status == PaymentStatus.Succeeded)
        {
            earning = CourierEarning.CreatePending(
                courierId,
                orderId,
                roundedAmount,
                normalizedCurrency,
                deliveredAtUtc,
                deliveredAtUtc.Add(options.HoldPeriod));

            balance.AddPending(roundedAmount, normalizedCurrency);
        }
        else
        {
            earning = CourierEarning.CreateBlocked(
                courierId,
                orderId,
                roundedAmount,
                normalizedCurrency,
                deliveredAtUtc);
        }

        await db.CourierEarnings.AddAsync(earning, ct);
        await db.SaveChangesAsync(ct);
    }

    public async Task ReleaseHeldEarningAsync(Guid orderId, CancellationToken ct = default)
    {
        var earning = await db.CourierEarnings
            .FirstOrDefaultAsync(x => x.OrderId == orderId, ct);

        if (earning is null || earning.Status != CourierEarningStatus.Blocked)
            return;

        var balance = await GetOrCreateBalanceAsync(earning.CourierId, earning.Currency, ct);
        earning.Activate(DateTime.UtcNow.Add(options.HoldPeriod));
        balance.AddPending(earning.Amount, earning.Currency);

        await db.SaveChangesAsync(ct);
    }

    public async Task VoidOrderEarningAsync(Guid orderId, CancellationToken ct = default)
    {
        var earning = await db.CourierEarnings
            .FirstOrDefaultAsync(x => x.OrderId == orderId, ct);

        if (earning is null || earning.Status is CourierEarningStatus.Paid or CourierEarningStatus.Voided)
            return;

        if (earning.Status == CourierEarningStatus.Pending)
        {
            var balance = await GetOrCreateBalanceAsync(earning.CourierId, earning.Currency, ct);
            balance.RemovePending(earning.Amount, earning.Currency);
        }

        if (earning.Status == CourierEarningStatus.Processing)
        {
            logger.LogWarning("Courier earning for order {OrderId} is already processing payout; skip void.", orderId);
            return;
        }

        earning.Void();
        await db.SaveChangesAsync(ct);
    }

    public async Task UpsertCourierStripeAccountAsync(Guid courierId, string stripeAccountId, CancellationToken ct = default)
    {
        var balance = await GetOrCreateBalanceAsync(courierId, options.DefaultCurrency, ct);
        balance.SetStripeAccount(stripeAccountId);
        await db.SaveChangesAsync(ct);
    }

    public async Task<CourierBalanceSnapshot> GetBalanceAsync(Guid courierId, CancellationToken ct = default)
    {
        var balance = await db.CourierBalances
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.CourierId == courierId, ct);

        if (balance is null)
        {
            return new CourierBalanceSnapshot(
                courierId,
                PendingAmount: 0m,
                AvailableAmount: 0m,
                Currency: NormalizeCurrency(options.DefaultCurrency),
                StripeAccountId: null,
                PayoutsEnabled: false);
        }

        return new CourierBalanceSnapshot(
            balance.CourierId,
            balance.PendingAmount,
            balance.AvailableAmount,
            balance.Currency,
            balance.StripeAccountId,
            balance.PayoutsEnabled);
    }

    public async Task<int> ProcessDuePayoutsAsync(CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        var dueEarnings = await db.CourierEarnings
            .Where(x =>
                x.Status == CourierEarningStatus.Pending
                && x.AvailableAtUtc != null
                && x.AvailableAtUtc <= now)
            .OrderBy(x => x.AvailableAtUtc)
            .ToListAsync(ct);

        if (dueEarnings.Count == 0)
            return 0;

        var payoutsProcessed = 0;

        foreach (var group in dueEarnings.GroupBy(x => new { x.CourierId, x.Currency }))
        {
            var totalAmount = decimal.Round(group.Sum(x => x.Amount), 2, MidpointRounding.AwayFromZero);
            if (totalAmount < options.MinimumPayoutAmount)
                continue;

            var balance = await db.CourierBalances
                .FirstOrDefaultAsync(x => x.CourierId == group.Key.CourierId, ct);

            if (balance is null || !balance.PayoutsEnabled || string.IsNullOrWhiteSpace(balance.StripeAccountId))
                continue;

            var payout = CourierPayout.Create(group.Key.CourierId, totalAmount, group.Key.Currency);
            await db.CourierPayouts.AddAsync(payout, ct);

            foreach (var earning in group)
            {
                earning.MarkProcessing(payout.Id);
            }

            balance.Reserve(totalAmount, group.Key.Currency);
            await db.SaveChangesAsync(ct);

            try
            {
                var transferId = await stripe.TransferToConnectedAccountAsync(
                    payout.Id,
                    group.Key.CourierId,
                    balance.StripeAccountId!,
                    new Money(totalAmount, group.Key.Currency),
                    ct);

                var paidAtUtc = DateTime.UtcNow;
                payout.MarkPaid(transferId, paidAtUtc);

                foreach (var earning in group)
                {
                    earning.MarkPaid(payout.Id, transferId, paidAtUtc);
                }

                balance.CompleteReserved(totalAmount, group.Key.Currency);
                await db.SaveChangesAsync(ct);

                payoutsProcessed += 1;
            }
            catch (Exception ex)
            {
                payout.MarkFailed(ex.Message, DateTime.UtcNow);

                foreach (var earning in group)
                {
                    earning.Reopen();
                }

                balance.RevertReserved(totalAmount, group.Key.Currency);
                await db.SaveChangesAsync(ct);

                logger.LogError(ex,
                    "Courier payout failed. CourierId={CourierId}, PayoutId={PayoutId}",
                    group.Key.CourierId,
                    payout.Id);
            }
        }

        return payoutsProcessed;
    }

    private async Task<CourierBalance> GetOrCreateBalanceAsync(Guid courierId, string currency, CancellationToken ct)
    {
        var balance = await db.CourierBalances
            .FirstOrDefaultAsync(x => x.CourierId == courierId, ct);

        if (balance is not null)
            return balance;

        balance = CourierBalance.Create(courierId, currency);
        await db.CourierBalances.AddAsync(balance, ct);
        return balance;
    }

    private static string NormalizeCurrency(string currency)
    {
        if (string.IsNullOrWhiteSpace(currency))
            throw new ArgumentException("Currency is required.", nameof(currency));

        return currency.Trim().ToUpperInvariant();
    }
}
