using System.Net;
using DF.UserService.Application.Services.Interfaces;
using DF.UserService.Contracts.Models.DTO;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Stripe;

namespace DF.UserService.Application.Services;


public class StripeConnectService : IStripeConnectService
{
    private readonly StripeClient _client;
    private readonly ILogger<StripeConnectService> _logger;

    public StripeConnectService(IOptions<StripeOptions> options, ILogger<StripeConnectService> logger)
    {
        if (string.IsNullOrWhiteSpace(options.Value.SecretKey))
            throw new InvalidOperationException("Stripe SecretKey is not configured.");

        _client = new StripeClient(options.Value.SecretKey);
        _logger = logger;
    }

    public async Task<string> CreateExpressAccountAsync(string email, string country, string? idempotencyKey = null, CancellationToken ct = default)
    {
        var service = new Stripe.AccountService(_client);
        var create = new AccountCreateOptions
        {
            Type = "express",
            Country = country,
            Email = email
        };

        var req = new RequestOptions
        {
            IdempotencyKey = idempotencyKey ?? $"acc_create_{email}_{country}".ToLowerInvariant()
        };

        var acc = await ExecuteWithRetryAsync(
            () => service.CreateAsync(create, req, ct), ct);

        return acc.Id;
    }

    public async Task<string> CreateOnboardingLinkAsync(string accountId, string returnUrl, string refreshUrl, CancellationToken ct = default)
    {
        var service = new AccountLinkService(_client);
        var create = new AccountLinkCreateOptions
        {
            Account = accountId,
            Type = "account_onboarding",
            ReturnUrl = returnUrl,
            RefreshUrl = refreshUrl
        };

        var link = await ExecuteWithRetryAsync(
            () => service.CreateAsync(create, requestOptions: null, ct), ct);

        return link.Url;
    }

    public async Task<StripeAccountStatusDto> GetAccountStatusAsync(string accountId, CancellationToken ct = default)
    {
        var service = new Stripe.AccountService(_client);
        var acc = await ExecuteWithRetryAsync(
            () => service.GetAsync(id: accountId, null, requestOptions: null, ct), ct);

        var due = acc.Requirements?.CurrentlyDue is { Count: > 0 }
            ? string.Join(',', acc.Requirements.CurrentlyDue)
            : string.Empty;

        return new StripeAccountStatusDto(acc.ChargesEnabled, acc.PayoutsEnabled, due);
    }

    public async Task<string> CreateLoginLinkAsync(string accountId, CancellationToken ct = default)
    {
        var service = new AccountLoginLinkService(_client);

        var link = await ExecuteWithRetryAsync(
            () => service.CreateAsync(accountId, null, requestOptions: null, ct),
            ct);

        return link.Url;
    }
    
    // -----------------
    // Helpers
    // -----------------
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
                await Task.Delay(WithJitter(delay), ct);
                delay = TimeSpan.FromMilliseconds(delay.TotalMilliseconds * 2);
            }
        }
        return await action(); // остання спроба (кине StripeException як є)
    }

    private static bool IsTransient(StripeException ex)
    {
        if (ex.HttpStatusCode is HttpStatusCode.TooManyRequests) return true;
        if (ex.HttpStatusCode is >= HttpStatusCode.InternalServerError) return true;
        return false;
    }

    private static TimeSpan WithJitter(TimeSpan baseDelay)
    {
        var jitter = Random.Shared.Next(50, 200);
        return baseDelay + TimeSpan.FromMilliseconds(jitter);
    }
}
