using System.Text;
using DF.PaymentService.Application.Common.Interfaces;
using DF.PaymentService.Application.Repositories.Interfaces;
using DF.PaymentService.Contracts;
using DF.PaymentService.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Stripe;

namespace DF.PaymentService.API.Controllers;

[ApiController]
[Route("webhooks/stripe/main")]
public class StripeWebhookController(
    IPaymentRepository payments,
    IOptions<StripeOptions> options,
    IProcessedWebhookStore webhookStore,
    ILogger<StripeWebhookController> logger)
    : ControllerBase
{
    private readonly string _endpointSecret = options.Value.WebhookSecret;

    [HttpPost]
    public async Task<IActionResult> Handle()
    {
        var json = await new StreamReader(Request.Body, Encoding.UTF8).ReadToEndAsync();
        var sigHeader = Request.Headers["Stripe-Signature"];

        Event stripeEvent;
        try
        {
            stripeEvent = EventUtility.ConstructEvent(json, sigHeader, _endpointSecret);
        }
        catch (StripeException ex)
        {
            logger.LogWarning(ex, "Stripe signature validation failed.");
            return BadRequest();
        }

        // Ідемпотентність
        if (await webhookStore.ExistsAsync(stripeEvent.Id, HttpContext.RequestAborted))
            return Ok();

        try
        {
            switch (stripeEvent.Type)
            {
                case "payment_intent.succeeded":
                    await OnPaymentIntentSucceeded((PaymentIntent)stripeEvent.Data.Object);
                    break;

                case "payment_intent.payment_failed":
                    await OnPaymentIntentFailed((PaymentIntent)stripeEvent.Data.Object);
                    break;

                case "payment_intent.canceled":
                    await OnPaymentIntentCanceled((PaymentIntent)stripeEvent.Data.Object);
                    break;

                case "refund.succeeded":
                    await OnRefundSucceeded((Refund)stripeEvent.Data.Object);
                    break;

                case "charge.refunded":
                    await OnChargeRefunded((Charge)stripeEvent.Data.Object);
                    break;

                case "charge.dispute.created":
                case "charge.dispute.updated":
                case "charge.dispute.closed":
                    await OnDisputeChanged((Dispute)stripeEvent.Data.Object);
                    break;

                default:
                    // інші події поки ігноруємо
                    break;
            }

            // Позначаємо як опрацьований лише після успішної обробки
            await webhookStore.MarkProcessedAsync(stripeEvent.Id, HttpContext.RequestAborted);
            return Ok();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error while processing Stripe webhook {EventType} {EventId}", stripeEvent.Type, stripeEvent.Id);
            // 200 ок з точки зору Stripe небажаний при фейлі обробки — краще 500, щоб Stripe ретраїв.
            return StatusCode(500);
        }
    }

    // ------------------------
    // Handlers
    // ------------------------

    private async Task OnPaymentIntentSucceeded(PaymentIntent intent)
    {
        var payment = await GetPaymentFromIntentAsync(intent);
        if (payment is null) return;

        if (payment.Status != PaymentStatus.Succeeded)
        {
            payment.MarkSucceeded();
            await payments.SaveChangesAsync(HttpContext.RequestAborted);
        }
    }

    private async Task OnPaymentIntentFailed(PaymentIntent intent)
    {
        var payment = await GetPaymentFromIntentAsync(intent);
        if (payment is null) return;

        var reason = intent.LastPaymentError?.Code ?? intent.LastPaymentError?.Message ?? "payment_failed";
        if (payment.Status != PaymentStatus.Failed)
        {
            payment.FailWithReason(reason);
            await payments.SaveChangesAsync(HttpContext.RequestAborted);
        }
    }

    private async Task OnPaymentIntentCanceled(PaymentIntent intent)
    {
        var payment = await GetPaymentFromIntentAsync(intent);
        if (payment is null) return;

        if (payment.Status is not PaymentStatus.Cancelled and not PaymentStatus.Refunded)
        {
            payment.CancelWithReason("stripe_canceled");
            await payments.SaveChangesAsync(HttpContext.RequestAborted);
        }
    }

    private async Task OnRefundSucceeded(Refund refund)
    {
        // Refund може не містити metadata.payment_id. Тоді шукаємо по PaymentIntentId.
        Payment? payment = null;

        var paymentIdFromMetadata =
            refund.Metadata != null && refund.Metadata.TryGetValue("payment_id", out var pidStr) && Guid.TryParse(pidStr, out var pid)
                ? pid
                : (Guid?)null;

        if (paymentIdFromMetadata.HasValue)
        {
            payment = await payments.GetByIdAsync(paymentIdFromMetadata.Value, HttpContext.RequestAborted);
        }
        else if (!string.IsNullOrWhiteSpace(refund.PaymentIntentId))
        {
            // Рекомендовано додати в репозиторій:
            // Task<Payment?> GetByStripePaymentIntentIdAsync(string piId, CancellationToken ct);
            payment = await payments.GetByStripePaymentIntentIdAsync(refund.PaymentIntentId, HttpContext.RequestAborted);
        }

        if (payment is null) return;

        // Out-of-order guard: коректно поводимось, якщо Succeeded ще не настав.
        if (payment.Status != PaymentStatus.Succeeded)
        {
            // Лог: отримали refund до succeeded — ігноруємо/відкладаємо (на практиці таких кейсів майже немає)
            return;
        }

        // Сума Refund-а у найменших одиницях
        if (refund.Amount is long amountMinor && amountMinor > 0)
        {
            var amount = FromMinorUnits(amountMinor, payment.Amount.Currency);

            // Якщо це повний рефанд: сума = увесь залишок
            var remaining = payment.Amount - payment.TotalRefunded;
            if (amount == remaining)
            {
                payment.ApplyFullRefund(refund.Id);
            }
            else
            {
                payment.ApplyPartialRefund(amount, refund.Id);
            }

            await payments.SaveChangesAsync(HttpContext.RequestAborted);
        }
    }

    private async Task OnChargeRefunded(Charge charge)
    {
        // Charge містить: PaymentIntentId, AmountRefunded, Refunded(bool)
        if (string.IsNullOrWhiteSpace(charge.PaymentIntentId))
            return;

        var payment = await payments.GetByStripePaymentIntentIdAsync(charge.PaymentIntentId, HttpContext.RequestAborted);
        if (payment is null) return;

        if (payment.Status != PaymentStatus.Succeeded)
        {
            // Out-of-order guard (див. нотатку в OnRefundSucceeded)
            return;
        }

        var amountRefundedMinor = charge.AmountRefunded;
        if (amountRefundedMinor <= 0) return;

        var totalRefundedMoney = FromMinorUnits(amountRefundedMinor, payment.Amount.Currency);

        // Обчислюємо дельту до вже відомого TotalRefunded
        if (totalRefundedMoney > payment.TotalRefunded)
        {
            var delta = totalRefundedMoney - payment.TotalRefunded;
            var remaining = payment.Amount - payment.TotalRefunded;

            if (delta == remaining)
                payment.ApplyFullRefund(); // stripeRefundId невідомий тут
            else
                payment.ApplyPartialRefund(delta);

            await payments.SaveChangesAsync(HttpContext.RequestAborted);
        }
    }

    private async Task OnDisputeChanged(Dispute dispute)
    {
        // Можемо знайти payment по charge → payment_intent
        var piId = dispute.Charge?.PaymentIntentId;
        if (string.IsNullOrWhiteSpace(piId)) return;

        var payment = await payments.GetByStripePaymentIntentIdAsync(piId, HttpContext.RequestAborted);
        if (payment is null) return;

        var status = dispute.Status; // e.g., 'needs_response', 'won', 'lost' ...
        if (string.IsNullOrWhiteSpace(payment.DisputeId))
        {
            payment.MarkDisputed(dispute.Id, status);
        }
        else
        {
            payment.UpdateDisputeStatus(status);
        }

        await payments.SaveChangesAsync(HttpContext.RequestAborted);
    }

    // ------------------------
    // Helpers
    // ------------------------

    private async Task<Payment?> GetPaymentFromIntentAsync(PaymentIntent intent)
    {
        // 1) Надійний шлях — metadata.payment_id
        if (intent.Metadata != null &&
            intent.Metadata.TryGetValue("payment_id", out var pidStr) &&
            Guid.TryParse(pidStr, out var pid))
        {
            return await payments.GetByIdAsync(pid, HttpContext.RequestAborted);
        }

        // 2) Якщо metadata немає → шукаємо по StripePaymentIntentId
        if (!string.IsNullOrWhiteSpace(intent.Id))
        {
            return await payments.GetByStripePaymentIntentIdAsync(intent.Id, HttpContext.RequestAborted);
        }

        return null;
    }

    private static Money FromMinorUnits(long amountMinor, string currency)
    {
        var scale = currency.ToUpperInvariant() switch
        {
            "JPY" or "KRW" => 0,
            _ => 2
        };
        var amount = amountMinor / (decimal)Math.Pow(10, scale);
        return new Money(amount, currency);
    }
}