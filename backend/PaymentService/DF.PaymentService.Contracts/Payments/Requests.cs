namespace DF.PaymentService.Contracts.Payments;

public sealed class RefundRequest
{
    /// <summary>
    /// Якщо null — повний рефанд; якщо > 0 — частковий (у валюті платежу).
    /// </summary>
    public decimal? Amount { get; set; }
}

public sealed class CancelPaymentRequest
{
    public string? Reason { get; set; }
}