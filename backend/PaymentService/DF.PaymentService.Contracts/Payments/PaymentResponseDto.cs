using DF.PaymentService.Domain.Entities;

namespace DF.PaymentService.Contracts.Payments;

public class PaymentResponseDto
{
    public Guid PaymentId { get; init; }
    public Guid OrderId { get; init; }
    public string Method { get; init; } = default!;
    public string Status { get; init; } = default!;
    public decimal Amount { get; init; }
    public string Currency { get; init; } = default!;
    public decimal TotalRefunded { get; init; }
    public DateTime? ExpiresAt { get; init; }

    // Видаємо client_secret ТІЛЬКИ для Online і ТІЛЬКИ коли потрібно на фронті (Pending/RequiresAction)
    public string? ClientSecret { get; init; }

    // Коротка історія (опційно)
    public IReadOnlyCollection<RefundItemDto> Refunds { get; init; } = Array.Empty<RefundItemDto>();

    public static PaymentResponseDto FromDomain(Payment p)
    {
        var exposeClientSecret =
            p.Method == PaymentMethod.Online &&
            (p.Status == PaymentStatus.Pending || p.Status == PaymentStatus.RequiresAction);

        return new PaymentResponseDto
        {
            PaymentId = p.Id,
            OrderId = p.OrderId,
            Method = p.Method.ToString(),
            Status = p.Status.ToString(),
            Amount = p.Amount.Amount,
            Currency = p.Amount.Currency,
            TotalRefunded = p.TotalRefunded.Amount,
            ExpiresAt = p.ExpiresAt,
            ClientSecret = exposeClientSecret ? p.StripeClientSecret : null,
            Refunds = p.Refunds.Select(r => new RefundItemDto
            {
                Id = r.Id,
                Amount = r.Amount.Amount,
                Currency = r.Amount.Currency,
                OccurredOnUtc = r.OccurredOnUtc,
                StripeRefundId = r.StripeRefundId
            }).ToArray()
        };
    }
}

public class RefundItemDto
{
    public Guid Id { get; init; }
    public decimal Amount { get; init; }
    public string Currency { get; init; } = default!;
    public DateTime OccurredOnUtc { get; init; }
    public string? StripeRefundId { get; init; }
}
