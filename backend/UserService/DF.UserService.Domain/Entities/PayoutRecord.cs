namespace DF.UserService.Domain.Entities;

// UserService.Domain/Entities/PayoutRecord.cs
public sealed class PayoutRecord
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid BusinessId { get; set; }                   // наш бізнес (ресторан)
    public string StripeAccountId { get; set; } = default!;// підключений акаунт
    public string StripePayoutId { get; set; } = default!; // payout.id
    public long AmountMinor { get; set; }                  // сума у minor units
    public string Currency { get; set; } = "usd";
    public string Status { get; set; } = "created";        // created/paid/failed/canceled...
    public DateTime CreatedAtUtc { get; set; }             // з події payout.created
    public DateTime? EstimatedArrivalUtc { get; set; }     // з payout. arrival_date
    public DateTime? PaidAtUtc { get; set; }               // коли став paid
    public DateTime? FailedAtUtc { get; set; }             // коли став failed
    public string? FailureCode { get; set; }               // failure_code
    public string? FailureMessage { get; set; }            // failure_message
    public string? BalanceTransactionId { get; set; }      // payout.balance_transaction
}