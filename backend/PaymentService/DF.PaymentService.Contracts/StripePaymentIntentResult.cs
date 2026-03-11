namespace DF.PaymentService.Contracts;


public record StripePaymentIntentResult(
    string PaymentIntentId,
    string ClientSecret);
