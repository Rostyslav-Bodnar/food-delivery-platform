using DF.UserService.Contracts.Models.Response;

namespace DF.UserService.Application.Services.Interfaces;

public interface IBusinessDashboardService
{
    /// <summary>
    /// Build a live financial dashboard for the given business by querying
    /// its Stripe Connect account directly. Returns null if the business
    /// has not finished Stripe onboarding yet.
    /// </summary>
    Task<BusinessDashboardResponse?> GetDashboardAsync(
        Guid businessId,
        DateTime fromUtc,
        DateTime toUtc,
        CancellationToken ct = default);

    /// <summary>
    /// Initiates a manual payout of the connected account's full available
    /// balance to the business's external bank account. Throws if the
    /// business has not finished Stripe onboarding, payouts are disabled,
    /// or available balance is zero.
    /// </summary>
    Task<ManualPayoutResponse> CreateManualPayoutAsync(
        Guid businessId,
        Guid requestingUserId,
        CancellationToken ct = default);
}
