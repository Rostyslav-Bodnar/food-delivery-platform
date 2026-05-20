using System.Text;
using DF.UserService.Application.Repositories.Interfaces;
using DF.UserService.Application.Services.Interfaces;
using DF.UserService.Contracts.Models.DTO;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Stripe;

namespace DF.UserService.API.Controllers;

[ApiController]
[Route("webhooks/stripe/connect")]
public class StripeConnectWebhookController(
    IAccountRepository accounts,
    IProcessedWebhookStore webhookStore,
    IOptions<StripeOptions> options) : ControllerBase
{
    private readonly string? _endpointSecret = options.Value.WebhookSecretConnect;

    [HttpPost]
    public async Task<IActionResult> Handle()
    {
        var json = await new StreamReader(Request.Body, Encoding.UTF8).ReadToEndAsync();
        var signature = Request.Headers["Stripe-Signature"];

        Event stripeEvent;
        try
        {
            stripeEvent = EventUtility.ConstructEvent(
                json, signature, _endpointSecret, throwOnApiVersionMismatch: false);
        }
        catch (StripeException)
        {
            return BadRequest();
        }

        if (await webhookStore.ExistsAsync(stripeEvent.Id, HttpContext.RequestAborted))
        {
            return Ok();
        }

        try
        {
            if (stripeEvent.Type == "account.updated"
                && stripeEvent.Data.Object is Account account)
            {
                var business = await accounts.GetBusinessByStripeIdAsync(
                    account.Id, HttpContext.RequestAborted);

                if (business is not null)
                {
                    business.StripeChargesEnabled = account.ChargesEnabled;
                    business.StripePayoutsEnabled = account.PayoutsEnabled;
                    business.StripeRequirementsDue = account.Requirements?.CurrentlyDue is { Count: > 0 }
                        ? string.Join(',', account.Requirements.CurrentlyDue)
                        : string.Empty;

                    if (account.ChargesEnabled && business.StripeOnboardedAt is null)
                        business.StripeOnboardedAt = DateTime.UtcNow;

                    await accounts.Update(business);
                }
            }

            await webhookStore.MarkProcessedAsync(stripeEvent.Id, HttpContext.RequestAborted);
            return Ok();
        }
        catch
        {
            return StatusCode(500); // Stripe will retry
        }
    }
}
