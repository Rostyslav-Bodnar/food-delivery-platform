namespace DF.UserService.Contracts.Models.Response;

/// <summary>
/// Reply for GET /api/account/onboarding/{businessId}.
/// Status "ready" carries the Stripe-hosted onboarding URL.
/// Status "provisioning" tells the client the Stripe account isn't ready yet
/// (StripeAccountProvisioningWorker still in flight); RetryAfterSeconds is a
/// hint for when to retry.
/// </summary>
public record OnboardingLinkResponse(
    string Status,
    string? Url,
    int? RetryAfterSeconds)
{
    public const string ReadyStatus = "ready";
    public const string ProvisioningStatus = "provisioning";

    public static OnboardingLinkResponse Ready(string url) =>
        new(ReadyStatus, url, null);

    public static OnboardingLinkResponse Provisioning(int retryAfterSeconds = 15) =>
        new(ProvisioningStatus, null, retryAfterSeconds);
}
