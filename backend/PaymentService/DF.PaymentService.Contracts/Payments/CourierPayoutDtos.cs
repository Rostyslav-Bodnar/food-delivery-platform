namespace DF.PaymentService.Contracts.Payments;

public class CourierBalanceResponseDto
{
    public Guid CourierId { get; init; }
    public decimal PendingAmount { get; init; }
    public decimal AvailableAmount { get; init; }
    public string Currency { get; init; } = default!;
    public string? StripeAccountId { get; init; }
    public bool PayoutsEnabled { get; init; }
}

public class UpdateCourierPayoutAccountRequest
{
    public string StripeAccountId { get; init; } = default!;
}
