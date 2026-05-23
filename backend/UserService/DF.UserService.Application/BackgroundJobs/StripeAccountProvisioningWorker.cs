using DF.UserService.Application.Repositories.Interfaces;
using DF.UserService.Application.Services.Interfaces;
using DF.UserService.Contracts.Models.DTO;
using DF.UserService.Domain.Entities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DF.UserService.Application.BackgroundJobs;

public sealed class StripeAccountProvisioningWorker(
    IServiceScopeFactory scopeFactory,
    IStripeConnectService stripe,
    IOptions<StripeOptions> stripeOptions,
    ILogger<StripeAccountProvisioningWorker> logger) : BackgroundService
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(10);
    private static readonly TimeSpan StartupDelay = TimeSpan.FromSeconds(5);
    private static readonly TimeSpan BaseBackoff = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan MaxBackoff = TimeSpan.FromHours(1);
    private const int BatchSize = 20;
    private const int MaxAttempts = 10;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await Task.Delay(StartupDelay, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProvisionPendingAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Stripe provisioning loop failed");
            }

            try
            {
                await Task.Delay(PollInterval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                return;
            }
        }
    }

    private async Task ProvisionPendingAsync(CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var accounts = scope.ServiceProvider.GetRequiredService<IAccountRepository>();

        // The repository query enforces both the attempt cap and the
        // backoff window: a row whose last attempt is within `BaseBackoff`
        // is skipped this pass. Per-row backoff growth is computed before
        // each attempt below.
        var notAttemptedSince = DateTime.UtcNow - BaseBackoff;
        var pending = await accounts.GetPendingStripeAccountsAsync(
            BatchSize, MaxAttempts, notAttemptedSince, ct);

        if (pending.Count == 0)
        {
            return;
        }

        foreach (var business in pending)
        {
            if (ct.IsCancellationRequested)
            {
                return;
            }

            if (!ReadyForAttempt(business))
            {
                continue;
            }

            await TryProvisionAsync(business, accounts, ct);
        }
    }

    private async Task TryProvisionAsync(
        BusinessAccount business,
        IAccountRepository accounts,
        CancellationToken ct)
    {
        business.StripeProvisioningAttempts++;
        business.StripeProvisioningLastAttemptUtc = DateTime.UtcNow;

        var email = business.User?.Email;
        if (string.IsNullOrWhiteSpace(email))
        {
            business.StripeProvisioningLastError = "User email is missing";
            logger.LogWarning(
                "Business {BusinessId} has no user email; attempt {Attempt}/{Max}",
                business.Id, business.StripeProvisioningAttempts, MaxAttempts);
            await accounts.Update(business);
            return;
        }

        try
        {
            var idempotencyKey = $"acc_create_{business.Id:N}";

            var stripeAccountId = await stripe.CreateExpressAccountAsync(
                email,
                stripeOptions.Value.DefaultCountry,
                idempotencyKey,
                ct);

            var status = await stripe.GetAccountStatusAsync(stripeAccountId, ct);

            business.StripeAccountId = stripeAccountId;
            business.StripeChargesEnabled = status.ChargesEnabled;
            business.StripePayoutsEnabled = status.PayoutsEnabled;
            business.StripeRequirementsDue = status.RequirementsDue;
            business.StripeProvisioningLastError = null;

            await accounts.Update(business);

            logger.LogInformation(
                "Provisioned Stripe Express account {StripeAccountId} for business {BusinessId} on attempt {Attempt}",
                stripeAccountId, business.Id, business.StripeProvisioningAttempts);
        }
        catch (Exception ex)
        {
            business.StripeProvisioningLastError = Truncate(ex.Message, 500);

            try
            {
                await accounts.Update(business);
            }
            catch (Exception persistEx)
            {
                logger.LogError(persistEx,
                    "Failed to persist provisioning failure for business {BusinessId}",
                    business.Id);
            }

            if (business.StripeProvisioningAttempts >= MaxAttempts)
            {
                logger.LogError(ex,
                    "Business {BusinessId} reached max provisioning attempts ({Max}); will not retry",
                    business.Id, MaxAttempts);
            }
            else
            {
                logger.LogWarning(ex,
                    "Stripe provisioning failed for business {BusinessId}; attempt {Attempt}/{Max}, next attempt in {Backoff}",
                    business.Id,
                    business.StripeProvisioningAttempts,
                    MaxAttempts,
                    NextBackoff(business.StripeProvisioningAttempts));
            }
        }
    }

    private static bool ReadyForAttempt(BusinessAccount business)
    {
        if (business.StripeProvisioningLastAttemptUtc is null)
        {
            return true;
        }

        var nextAt = business.StripeProvisioningLastAttemptUtc.Value
                     + NextBackoff(business.StripeProvisioningAttempts);

        return DateTime.UtcNow >= nextAt;
    }

    private static TimeSpan NextBackoff(int attempts)
    {
        if (attempts <= 0)
        {
            return BaseBackoff;
        }

        // Exponential: 30s, 60s, 2m, 4m, 8m, ... capped at 1h.
        var ticks = BaseBackoff.Ticks * (long)Math.Pow(2, Math.Min(attempts, 12));
        if (ticks > MaxBackoff.Ticks || ticks < 0)
        {
            return MaxBackoff;
        }
        return new TimeSpan(ticks);
    }

    private static string Truncate(string value, int max) =>
        value.Length <= max ? value : value[..max];
}
