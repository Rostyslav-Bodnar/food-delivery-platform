using System.Text;
using DF.UserService.Application.Repositories.Interfaces;
using DF.UserService.Application.Services.Interfaces;
using DF.UserService.Contracts.Exceptions;
using DF.UserService.Contracts.Models.DTO;
using DF.UserService.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Stripe;

namespace DF.UserService.API.Controllers;

[ApiController]
[Route("webhooks/stripe/payout")]
public sealed class StripePayoutWebhookController(
    IAccountRepository accountRepository,
    IPayoutRepository payoutRepository,
    IOptions<StripeOptions> options,
    IProcessedWebhookStore webhookStore) : ControllerBase
{
    private readonly string _endpointSecret = options.Value.WebhookSecretPayout!;

    [HttpPost]
    public async Task<IActionResult> Handle()
    {
        var json = await new StreamReader(Request.Body, Encoding.UTF8).ReadToEndAsync();
        var signature = Request.Headers["Stripe-Signature"];

        Event stripeEvent;
        try
        {
            stripeEvent = EventUtility.ConstructEvent(json, signature, _endpointSecret);
        }
        catch
        {
            return BadRequest();
        }

        // ✅ Ідемпотентність
        if (await webhookStore.ExistsAsync(stripeEvent.Id, HttpContext.RequestAborted))
            return Ok();

        try
        {
            switch (stripeEvent.Type)
            {
                case "payout.created":
                    await OnPayoutCreated((Payout)stripeEvent.Data.Object, stripeEvent.Account);
                    break;

                case "payout.paid":
                    await OnPayoutPaid((Payout)stripeEvent.Data.Object, stripeEvent.Account);
                    break;

                case "payout.failed":
                    await OnPayoutFailed((Payout)stripeEvent.Data.Object, stripeEvent.Account);
                    break;

                default:
                    break;
            }

            await webhookStore.MarkProcessedAsync(stripeEvent.Id, HttpContext.RequestAborted);
            return Ok();
        }
        catch
        {
            return StatusCode(500); // Stripe зробить retry
        }
    }

    private static DateTime ToUtc(long? unixSeconds)
        => unixSeconds is null
            ? DateTime.UtcNow
            : DateTimeOffset.FromUnixTimeSeconds(unixSeconds.Value).UtcDateTime;

    private async Task<BusinessAccount?> GetBusiness(string stripeAccountId)
    {
        return await accountRepository.GetBusinessByStripeIdAsync(stripeAccountId, HttpContext.RequestAborted);
    }

    private async Task<PayoutRecord> EnsureCreatedAsync(Payout payout, string stripeAccountId)
    {
        var existing = await payoutRepository.GetByStripePayoutIdAsync(payout.Id, HttpContext.RequestAborted);
        if (existing is not null) return existing;

        var business = await GetBusiness(stripeAccountId);
        if (business is null)
            // NotFound → 404 → Stripe acks the webhook (won't retry forever). A bare Exception → 500 → endless retries.
            throw new NotFoundException($"Business for StripeAccountId={stripeAccountId} not found");

        var rec = new PayoutRecord
        {
            BusinessId = business.Id,
            StripeAccountId = stripeAccountId,
            StripePayoutId = payout.Id,
            AmountMinor = payout.Amount,
            Currency = payout.Currency?.ToUpperInvariant() ?? "USD",
            Status = payout.Status,
            CreatedAtUtc = payout.Created,
            EstimatedArrivalUtc = payout.ArrivalDate,
            BalanceTransactionId = payout.BalanceTransactionId
        };

        await payoutRepository.AddAsync(rec, HttpContext.RequestAborted);
        await payoutRepository.SaveChangesAsync(HttpContext.RequestAborted);

        return rec;
    }

    private async Task OnPayoutCreated(Payout payout, string stripeAccountId)
    {
        await EnsureCreatedAsync(payout, stripeAccountId);
    }

    private async Task OnPayoutPaid(Payout payout, string stripeAccountId)
    {
        var rec = await EnsureCreatedAsync(payout, stripeAccountId);

        rec.Status = "paid";
        rec.PaidAtUtc = payout.Created;
        rec.BalanceTransactionId = payout.BalanceTransactionId ?? rec.BalanceTransactionId;

        await payoutRepository.SaveChangesAsync(HttpContext.RequestAborted);
    }

    private async Task OnPayoutFailed(Payout payout, string stripeAccountId)
    {
        var rec = await EnsureCreatedAsync(payout, stripeAccountId);

        rec.Status = "failed";
        rec.FailedAtUtc = DateTime.UtcNow;
        rec.FailureCode = payout.FailureCode;
        rec.FailureMessage = payout.FailureMessage;

        await payoutRepository.SaveChangesAsync(HttpContext.RequestAborted);

        // 🔔 Можна додати push/email для ресторану
    }
}