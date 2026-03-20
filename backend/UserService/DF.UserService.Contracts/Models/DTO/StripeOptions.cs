namespace DF.UserService.Contracts.Models.DTO;

public class StripeOptions
{
    public string SecretKey { get; init; } = string.Empty;
    public string? WebhookSecretConnect { get; init; }
    public string DefaultCountry { get; init; } = "UA";

    public DashboardOptions Dashboard { get; init; } = new();
    public sealed class DashboardOptions
    {
        public string ReturnUrl { get; init; } = string.Empty;
        public string RefreshUrl { get; init; } = string.Empty;
    }
}
