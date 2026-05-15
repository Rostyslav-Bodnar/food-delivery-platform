namespace DF.PaymentService.Contracts;

public class StripeOptions
{
    public string SecretKey { get; init; } = default!;
    public string WebhookSecret { get; init; } = default!;
}
