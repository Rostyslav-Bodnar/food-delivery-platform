namespace DF.PaymentService.Domain.Entities
{
    public enum PaymentTaskType
    {
        CreateStripePaymentIntent = 1
    }

    public class PaymentTask
    {
        public Guid Id { get; set; }
        public Guid PaymentId { get; set; }
        public PaymentTaskType Type { get; set; }

        public int RetryCount { get; set; }
        public DateTime CreatedOnUtc { get; set; }
        public DateTime? NextAttemptUtc { get; set; }
        public DateTime? ProcessedOnUtc { get; set; }
        public string? Error { get; set; }

        public static PaymentTask Create(Guid paymentId, PaymentTaskType type)
            => new()
            {
                Id = Guid.NewGuid(),
                PaymentId = paymentId,
                Type = type,
                RetryCount = 0,
                CreatedOnUtc = DateTime.UtcNow,
                NextAttemptUtc = DateTime.UtcNow
            };
    }
}