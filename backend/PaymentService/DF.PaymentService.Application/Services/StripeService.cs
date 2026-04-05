using DF.PaymentService.Application.Services.Interfaces;
using DF.PaymentService.Contracts;
using DF.PaymentService.Domain.Entities;
using Microsoft.Extensions.Options;
using Stripe;
using System.Net;
using PaymentMethod = DF.PaymentService.Domain.Entities.PaymentMethod;

namespace DF.PaymentService.Application.Services;

public sealed class StripeService(IOptions<StripeOptions> options) : IStripeService
{
    private readonly StripeClient _client = new(options.Value.SecretKey);

    public async Task<StripePaymentIntentResult> CreatePaymentIntentAsync(Payment payment, CancellationToken ct = default)
    {
        var piService = new PaymentIntentService(_client);

        var amountMinor = ToMinorUnits(payment.Amount);

        var create = new PaymentIntentCreateOptions
        {
            Amount = amountMinor,
            Currency = payment.Amount.Currency.ToLowerInvariant(),
            AutomaticPaymentMethods = new PaymentIntentAutomaticPaymentMethodsOptions
            {
                Enabled = true
            },
            Metadata = new Dictionary<string, string>
            {
                ["payment_id"] = payment.Id.ToString(),
                ["order_id"]   = payment.OrderId.ToString()
            }
        };

        // Stripe ідемпотентність
        var req = new RequestOptions
        {
            IdempotencyKey = $"pi_{payment.Id}"
        };

        var intent = await ExecuteWithRetryAsync(
            () => piService.CreateAsync(create, req, ct),
            ct);

        return new StripePaymentIntentResult(intent.Id, intent.ClientSecret);
    }

    // ✅ Виправлено: рефандимо по реальному StripePaymentIntentId
    public async Task<string> RefundAsync(Payment payment, decimal? amount = null, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(payment.StripePaymentIntentId))
            throw new InvalidOperationException("Cannot refund: StripePaymentIntentId is missing.");

        var refundService = new RefundService(_client);

        var options = new RefundCreateOptions
        {
            PaymentIntent = payment.StripePaymentIntentId,
            Amount = amount.HasValue ? ToMinorUnits(new Money(amount.Value, payment.Amount.Currency)) : null
        };

        var refund = await ExecuteWithRetryAsync(
            () => refundService.CreateAsync(options, requestOptions: null, ct),
            ct);

        return refund.Id;
    }

    // ✅ Нове: скасування PaymentIntent (для сценаріїв user cancel / timeout)
    public async Task CancelPaymentIntentAsync(Payment payment, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(payment.StripePaymentIntentId))
            return; // Нема чого скасовувати

        var piService = new PaymentIntentService(_client);

        await ExecuteWithRetryAsync(
            () => piService.CancelAsync(payment.StripePaymentIntentId, cancellationToken: ct),
            ct);
    }

    // 🔄 Опційне: підтвердження з сервера (корисно для manual capture або контролю RA)
    public async Task<PaymentIntent> ConfirmPaymentIntentAsync(Payment payment,
        PaymentIntentConfirmOptions? options = null,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(payment.StripePaymentIntentId))
            throw new InvalidOperationException("Cannot confirm: StripePaymentIntentId is missing.");

        var piService = new PaymentIntentService(_client);

        var intent = await ExecuteWithRetryAsync(
            () => piService.ConfirmAsync(payment.StripePaymentIntentId, options, cancellationToken: ct),
            ct);

        return intent;
    }
    
    
    public async Task<StripePaymentIntentResult> CreateDestinationPaymentIntentAsync(
        Payment payment,
        string destinationStripeAccountId,
        decimal platformFeePercent = 0.05m,
        CancellationToken ct = default)
    {
        if (payment.Method != PaymentMethod.Online)
            throw new InvalidOperationException("Destination charge applicable only for Online payments.");

        // ідемпотентність: якщо PI вже створений — повертаємо його
        if (!string.IsNullOrWhiteSpace(payment.StripePaymentIntentId))
            return new StripePaymentIntentResult(payment.StripePaymentIntentId!, payment.StripeClientSecret!);

        var amountMinor = ToMinorUnits(payment.Amount);

        var scale = GetCurrencyScale(payment.Amount.Currency);
        var feeMinor = (long)Math.Round(
            payment.Amount.Amount * platformFeePercent * (decimal)Math.Pow(10, scale),
            MidpointRounding.AwayFromZero);

        var piService = new PaymentIntentService(_client);

        var create = new PaymentIntentCreateOptions
        {
            Amount = amountMinor,
            Currency = payment.Amount.Currency.ToLowerInvariant(),
            AutomaticPaymentMethods = new PaymentIntentAutomaticPaymentMethodsOptions { Enabled = true },

            // ⭐️ Destination charges
            TransferData = new PaymentIntentTransferDataOptions { Destination = destinationStripeAccountId },
            ApplicationFeeAmount = feeMinor,

            Metadata = new Dictionary<string, string>
            {
                ["payment_id"] = payment.Id.ToString(),
                ["order_id"]   = payment.OrderId.ToString(),
                ["funds_flow"] = "destination"
            }
        };

        var req = new RequestOptions { IdempotencyKey = $"pi_dest_{payment.Id}" };

        var intent = await ExecuteWithRetryAsync(
            () => piService.CreateAsync(create, req, ct),
            ct);

        return new StripePaymentIntentResult(intent.Id, intent.ClientSecret);
    }

    // Рефанд для Destination: reverse_transfer = true (щоб Stripe витягнув частку з акаунта ресторану)
    public async Task<string> RefundDestinationAsync(Payment payment, decimal? amount = null, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(payment.StripePaymentIntentId))
            throw new InvalidOperationException("Cannot refund: StripePaymentIntentId is missing.");

        var refundService = new RefundService(_client);

        var options = new RefundCreateOptions
        {
            PaymentIntent = payment.StripePaymentIntentId,
            Amount = amount.HasValue ? ToMinorUnits(new Money(amount.Value, payment.Amount.Currency)) : null,
            ReverseTransfer = true // критично для Destination‑флоу
        };

        var refund = await ExecuteWithRetryAsync(
            () => refundService.CreateAsync(options, requestOptions: null, ct),
            ct);

        return refund.Id;
    }


    // ------------------------------
    // Helpers
    // ------------------------------

    private static long ToMinorUnits(Money money)
    {
        var scale = GetCurrencyScale(money.Currency);
        return (long)Math.Round(money.Amount * (decimal)Math.Pow(10, scale), MidpointRounding.AwayFromZero);
    }

    private static int GetCurrencyScale(string currency) =>
        currency.ToUpperInvariant() switch
        {
            "JPY" or "KRW" => 0,
            _ => 2
        };

    // Простий retry з експоненційною затримкою (429/5xx)
    private static async Task<T> ExecuteWithRetryAsync<T>(Func<Task<T>> action, CancellationToken ct, int maxAttempts = 3)
    {
        var delay = TimeSpan.FromMilliseconds(300);

        for (int attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                return await action();
            }
            catch (StripeException ex) when (IsTransient(ex) && attempt < maxAttempts)
            {
                await Task.Delay(Jitter(delay), ct);
                delay = TimeSpan.FromMilliseconds(delay.TotalMilliseconds * 2);
                continue;
            }
        }

        // остання спроба без перехоплення: хай пробросить оригінальну StripeException
        return await action();
    }

    private static TimeSpan Jitter(TimeSpan baseDelay)
    {
        var jitterMs = Random.Shared.Next(50, 150);
        return baseDelay + TimeSpan.FromMilliseconds(jitterMs);
    }

    private static bool IsTransient(StripeException ex)
    {
        // StripeException може містити HttpStatusCode (429/5xx — транзієнтні)
        if (ex.HttpStatusCode is HttpStatusCode.TooManyRequests) return true;
        if (ex.HttpStatusCode is >= HttpStatusCode.InternalServerError) return true;

        // stripe-dotnet також має StripeError/Code (можна доповнити за потреби)
        return false;
    }
}