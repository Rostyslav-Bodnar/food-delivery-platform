using DF.UserService.Contracts.Models.DTO;

namespace DF.UserService.Application.Services.Interfaces;

public interface IStripeConnectService
{
    /// <summary>Створити Connected Account (Express)</summary>
    Task<string> CreateExpressAccountAsync(string email, string country, string? idempotencyKey = null, CancellationToken ct = default);

    /// <summary>Згенерувати hosted onboarding link для завершення KYC/банківських реквізитів</summary>
    Task<string> CreateOnboardingLinkAsync(string accountId, string returnUrl, string refreshUrl, CancellationToken ct = default);

    /// <summary>Отримати базовий статус акаунта (charges/payouts/requirements)</summary>
    Task<StripeAccountStatusDto> GetAccountStatusAsync(string accountId, CancellationToken ct = default);

    /// <summary>Згенерувати login link до Stripe Express Dashboard (опційно)</summary>
    Task<string> CreateLoginLinkAsync(string accountId, CancellationToken ct = default);
}
